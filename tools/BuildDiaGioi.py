"""Dung bo du lieu dia gioi hanh chinh tu bang chuyen doi DVHC moi-cu cua Tong cuc Thong ke (NSO).

Nguon : data/dia-gioi/nguon/BangChuyenDoiDVHC_moi_cu_final.xlsx
        (https://danhmuchanhchinh.nso.gov.vn/TAPTIN/BangChuyendoiĐVHCmoi_cu_final.xlsx, tep cap nhat 23/10/2025)
Dau ra: data/dia-gioi/dvhc_cu.csv, dvhc_moi.csv, chuyen_doi.csv, bao_cao_lam_sach.txt
        src/HoTroDuLich.Web/Database/008c_NapDiaGioi.sql
Chay  : python tools/BuildDiaGioi.py   (chi dung thu vien chuan)
"""
import collections, csv, re, sys, unicodedata, zipfile
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / "data" / "dia-gioi" / "nguon" / "BangChuyenDoiDVHC_moi_cu_final.xlsx"
OUT = ROOT / "data" / "dia-gioi"
SQL = ROOT / "src" / "HoTroDuLich.Web" / "Database" / "008c_NapDiaGioi.sql"
NS = {"m": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
SOURCE_NAME = "Tổng cục Thống kê (NSO) - Bảng chuyển đổi đơn vị hành chính mới-cũ, tệp cập nhật 23/10/2025"
CHECKED_ON = "2026-11-10"
report = []


def read_sheet(path, sheet_hint):
    z = zipfile.ZipFile(path)
    ss = []
    for si in ET.fromstring(z.read("xl/sharedStrings.xml")).findall("m:si", NS):
        ss.append("".join(t.text or "" for t in si.iter("{%s}t" % NS["m"])))
    wb = ET.fromstring(z.read("xl/workbook.xml"))
    rels = {r.get("Id"): r.get("Target") for r in ET.fromstring(z.read("xl/_rels/workbook.xml.rels"))}
    part = None
    for s in wb.find("m:sheets", NS):
        if sheet_hint in s.get("name"):
            rid = s.get("{http://schemas.openxmlformats.org/officeDocument/2006/relationships}id")
            part = "xl/" + rels[rid].lstrip("/").replace("xl/", "")
    for _, el in ET.iterparse(z.open(part)):
        if el.tag == "{%s}row" % NS["m"]:
            row = {}
            for c in el.findall("m:c", NS):
                col = re.match(r"[A-Z]+", c.get("r")).group()
                v = c.find("m:v", NS)
                row[col] = ss[int(v.text)] if (v is not None and c.get("t") == "s") else (v.text if v is not None else "")
            yield int(el.get("r")), row
            el.clear()


def clean(s):
    return re.sub(r"\s+", " ", (s or "").replace("\xa0", " ")).strip()


def fold(s):
    t = unicodedata.normalize("NFD", s or "")
    t = "".join(ch for ch in t if unicodedata.category(ch) != "Mn")
    return t.replace("đ", "d").replace("Đ", "D").lower()


def slug(s):
    return re.sub(r"[^a-z0-9]+", "-", fold(s)).strip("-")


def split_code(label):
    m = re.search(r"\((\d+)\)\s*$", clean(label))
    return (clean(label[: m.start()]) if m else clean(label)), (m.group(1) if m else None)


PREFIX = {
    1: [("Thành phố", "ThanhPho"), ("Tỉnh", "Tinh")],
    2: [("Thành phố", "ThanhPhoThuocTinh"), ("Thị xã", "ThiXa"), ("Quận", "Quan"), ("Huyện", "Huyen")],
    3: [("Thị trấn", "ThiTran"), ("Phường", "Phuong"), ("Đặc khu", "DacKhu"), ("Xã", "Xa")],
}


def norm_case(s):
    return clean(s)


def classify(level, full):
    """Nhan dien loai theo tien to, khong phan biet hoa/thuong/dau; chuan hoa cach viet tien to."""
    full = clean(full)
    fl = fold(full)
    for pre, kind in PREFIX[level]:
        if fl.startswith(fold(pre) + " "):
            rest = full[len(pre) + 1:].strip()
            return kind, rest, pre + " " + rest
    return None, full, full


def main():
    rows = [r for n, r in read_sheet(SRC, "không merge") if n >= 3]
    body = [r for r in rows if r.get("B") or r.get("D")]
    report.append(f"Dong du lieu: {len(body)} (bo {len(rows) - len(body)} dong tieu de tinh)")

    # ---------- he thong MOI (34 tinh -> xa/phuong/dac khu) ----------
    new_prov, new_com_names = {}, collections.defaultdict(collections.Counter)
    new_com_parent = {}
    for r in body:
        pname, pcode = split_code(r["A"])
        new_prov[pcode] = norm_case(pname)
        new_com_names[clean(r["C"])][norm_case(r["B"])] += 1
        new_com_parent[clean(r["C"])] = pcode
    for code, c in new_com_names.items():
        if len(c) > 1:
            report.append(f"Ma xa moi {code} co nhieu ten, chon ten pho bien nhat: {dict(c)}")

    # ---------- he thong CU (63 tinh -> huyen -> xa) ----------
    old_prov, dist_prov, dist_name = {}, {}, {}
    dist_votes = collections.defaultdict(collections.Counter)
    unit_codes = collections.defaultdict(collections.Counter)  # (ma huyen, ten xa) -> dem ma
    for r in body:
        pname, pcode = split_code(r["H"])
        if pname == "Quảng Trị" and pcode == "44":  # nguon gan nham: Con Co thuoc Quang Tri (45)
            pname, pcode = "Tỉnh Quảng Trị", "45"
            report.append("Sua nhan tinh sai trong nguon: 'Quảng Trị (44)' -> 'Tỉnh Quảng Trị (45)' (huyen Con Co)")
        old_prov[pcode] = norm_case(pname)
        dname, dcode = split_code(r["G"])
        dist_name[dcode] = norm_case(dname)
        dist_votes[dcode][pcode] += 1
        if clean(r.get("E")):
            unit_codes[(dcode, clean(r["D"]))][clean(r["E"])] += 1

    for dcode, votes in dist_votes.items():
        dist_prov[dcode] = votes.most_common(1)[0][0]
        if len(votes) > 1:
            report.append(f"Huyen {dcode} ({dist_name[dcode]}) co nhan tinh khong thong nhat {dict(votes)} -> chon {dist_prov[dcode]} (da bo phieu da so)")

    # chon ma cho tung xa cu; xu ly ma bi gan trung (loi go trong nguon)
    assigned, taken = {}, {}
    for key, counter in sorted(unit_codes.items(), key=lambda kv: -kv[1].most_common(1)[0][1]):
        for code, _ in counter.most_common():
            if (key[0], code) not in taken:
                taken[(key[0], code)] = key
                assigned[key] = code
                break
        else:
            code = counter.most_common(1)[0][0] + "X"
            assigned[key] = code
            taken[(key[0], code)] = key
            report.append(f"CAN DOI CHIEU: xa {key[1]} (huyen {key[0]}) trung ma nguon {counter.most_common(1)[0][0]} voi xa khac -> ma tam {code}")
        if len(counter) > 1:
            report.append(f"Xa {key[1]} (huyen {key[0]}) co nhieu ma {dict(counter)} -> chon {assigned.get(key)}")

    # ---------- xuat danh sach don vi ----------
    units = []  # (regime, code, parent, level, kind, name, full)
    def add(regime, code, parent, level, full):
        kind, name, fullname = classify(level, full)
        if kind is None:
            kind = {1: "Tinh", 2: "Huyen", 3: "Xa"}[level]
            fullname = {1: "Tỉnh ", 2: "Huyện ", 3: "Xã "}[level] + name
            report.append(f"Thieu tien to loai, mac dinh '{fullname}': {regime} {code} '{full}'")
        units.append((regime, code, parent, level, kind, name, fullname))

    for pcode, pname in sorted(old_prov.items()):
        add("Cu", pcode, None, 1, pname)
    for dcode, dname in sorted(dist_name.items()):
        add("Cu", dcode, dist_prov[dcode], 2, dname)
    for (dcode, cname), code in sorted(assigned.items(), key=lambda kv: kv[1]):
        add("Cu", code, dcode, 3, cname)
    for pcode, pname in sorted(new_prov.items()):
        add("Moi", pcode, None, 1, pname)
    for code, counter in sorted(new_com_names.items()):
        add("Moi", code, new_com_parent[code], 3, counter.most_common(1)[0][0])

    # kiem tra
    seen = set()
    for u in units:
        if (u[0], u[1]) in seen:
            report.append(f"TRUNG MA {u[0]} {u[1]}")
        seen.add((u[0], u[1]))
    for regime in ("Cu", "Moi"):
        c = collections.Counter(u[3] for u in units if u[0] == regime)
        report.append(f"{regime}: tinh={c[1]} huyen={c[2]} xa/phuong/dac khu={c[3]}")
    maxlen = max(len(u[5]) for u in units), max(len(u[6]) for u in units)
    report.append(f"Do dai lon nhat: ten={maxlen[0]}, ten day du={maxlen[1]}")

    # ---------- anh xa cu -> moi ----------
    mapping = collections.OrderedDict()
    for r in body:
        dname, dcode = split_code(r["G"])
        newcode = clean(r["C"])
        note = clean(r.get("F"))
        if clean(r.get("E")):
            oldcode = assigned.get((dcode, clean(r["D"])))
        else:  # huyen chuyen thanh dac khu / khu vuc khong co xa cu
            oldcode = dcode
        if oldcode:
            mapping.setdefault((oldcode, newcode), note)
    report.append(f"Anh xa cu->moi: {len(mapping)} cap")

    # ---------- ghi CSV ----------
    OUT.mkdir(parents=True, exist_ok=True)
    for regime, fn in (("Cu", "dvhc_cu.csv"), ("Moi", "dvhc_moi.csv")):
        with open(OUT / fn, "w", encoding="utf-8-sig", newline="") as f:
            w = csv.writer(f)
            w.writerow(["Code", "ParentCode", "Level", "Kind", "Name", "FullName"])
            for u in units:
                if u[0] == regime:
                    w.writerow([u[1], u[2] or "", u[3], u[4], u[5], u[6]])
    with open(OUT / "chuyen_doi.csv", "w", encoding="utf-8-sig", newline="") as f:
        w = csv.writer(f)
        w.writerow(["OldCode", "NewCode", "Note"])
        for (o, n), note in mapping.items():
            w.writerow([o, n, note])
    (OUT / "bao_cao_lam_sach.txt").write_text("\n".join(report) + "\n", encoding="utf-8")

    # ---------- ghi script SQL nap ----------
    def lit(s):
        return "N'" + (s or "").replace("'", "''") + "'"

    out = []
    w = out.append
    w("-- SINH TU DONG boi tools/BuildDiaGioi.py - KHONG SUA TAY. Nguon: " + SOURCE_NAME + ".")
    w("-- BAN NHAP - CHUA CHAY TREN DATABASE CHINH. Chay SAU 008. Nap 'Cu' (63 tinh -> huyen -> xa) va 'Moi' (34 tinh -> xa) + anh xa.")
    w("SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET ARITHABORT ON; SET CONCAT_NULL_YIELDS_NULL ON; SET NUMERIC_ROUNDABORT OFF; SET XACT_ABORT ON;")
    w("BEGIN TRANSACTION;")
    w("DECLARE @lock int;")
    w("EXEC @lock=sys.sp_getapplock @Resource=N'HoTroDuLich.DiaGioi.v1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=30000;")
    w("IF @lock<0 THROW 51000,N'Không lấy được khóa nạp địa giới.',1;")
    w("IF EXISTS (SELECT 1 FROM dbo.DonViHanhChinh) THROW 51002,N'Bảng DonViHanhChinh đã có dữ liệu; không nạp lại.',1;")
    w("CREATE TABLE #S (Regime varchar(4) COLLATE DATABASE_DEFAULT NOT NULL, Code varchar(12) COLLATE DATABASE_DEFAULT NOT NULL, ParentCode varchar(12) COLLATE DATABASE_DEFAULT NULL, [Level] tinyint NOT NULL, Kind varchar(20) COLLATE DATABASE_DEFAULT NOT NULL, Name nvarchar(100) COLLATE DATABASE_DEFAULT NOT NULL, FullName nvarchar(150) COLLATE DATABASE_DEFAULT NOT NULL, NameFolded nvarchar(150) COLLATE Latin1_General_100_CI_AI NOT NULL, Slug varchar(160) COLLATE DATABASE_DEFAULT NOT NULL);")
    for i in range(0, len(units), 500):
        chunk = units[i:i + 500]
        w("INSERT #S (Regime,Code,ParentCode,[Level],Kind,Name,FullName,NameFolded,Slug) VALUES")
        vals = []
        for (regime, code, parent, level, kind, name, full) in chunk:
            sl = slug(full) if level == 1 else slug(full) + "-" + code
            vals.append(f"('{regime}','{code}',{('NULL' if parent is None else repr(parent))},{level},'{kind}',{lit(name)},{lit(full)},{lit(fold(name))},'{sl}')")
        w(",\n".join(vals) + ";")
    for level in (1, 2, 3):
        w(f"INSERT dbo.DonViHanhChinh (Regime,Code,ParentId,[Level],Kind,Name,FullName,NameFolded,Slug,Status,EffectiveFrom,EffectiveTo,SourceName,CheckedOn)")
        w(f"SELECT s.Regime,s.Code,p.Id,s.[Level],s.Kind,s.Name,s.FullName,s.NameFolded,s.Slug,'Active',"
          f"CASE s.Regime WHEN 'Moi' THEN '2025-07-01' ELSE '1900-01-01' END,CASE s.Regime WHEN 'Cu' THEN '2025-06-30' ELSE NULL END,{lit(SOURCE_NAME)},'{CHECKED_ON}'")
        w(f"FROM #S s LEFT JOIN dbo.DonViHanhChinh p ON p.Regime=s.Regime AND p.Code=s.ParentCode WHERE s.[Level]={level};")
        w(f"IF @@ROWCOUNT<>(SELECT COUNT(*) FROM #S WHERE [Level]={level}) THROW 51003,N'Số dòng nạp cấp {level} không khớp (thiếu đơn vị cha).',1;")
    w("CREATE TABLE #M (OldCode varchar(12) COLLATE DATABASE_DEFAULT NOT NULL, NewCode varchar(12) COLLATE DATABASE_DEFAULT NOT NULL, Note nvarchar(200) COLLATE DATABASE_DEFAULT NOT NULL);")
    items = list(mapping.items())
    for i in range(0, len(items), 500):
        w("INSERT #M (OldCode,NewCode,Note) VALUES")
        w(",\n".join(f"('{o}','{n}',{lit(note)})" for (o, n), note in items[i:i + 500]) + ";")
    w("INSERT dbo.DonViChuyenDoi (OldUnitId,NewUnitId,Note)")
    w("SELECT o.Id,n.Id,m.Note FROM #M m JOIN dbo.DonViHanhChinh o ON o.Regime='Cu' AND o.Code=m.OldCode JOIN dbo.DonViHanhChinh n ON n.Regime='Moi' AND n.Code=m.NewCode;")
    w(f"IF @@ROWCOUNT<>{len(items)} THROW 51004,N'Số dòng ánh xạ cũ-mới không khớp.',1;")
    w("COMMIT TRANSACTION;")
    SQL.write_text("\n".join(out) + "\n", encoding="utf-8-sig")
    print("\n".join(report))


if __name__ == "__main__":
    sys.exit(main())
