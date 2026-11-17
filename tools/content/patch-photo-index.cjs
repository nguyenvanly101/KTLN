// Anh do nha cung cap tai len (dich vu, co so) duoc dua vao muc tra cuu de the Kham pha hien anh.
const fs=require('fs'),path=require('path');const root=path.join(__dirname,'../../src/HoTroDuLich.Web');
const rd=f=>fs.readFileSync(path.join(root,f),'utf8').split('\r\n').join('\n'),wr=(f,s)=>fs.writeFileSync(path.join(root,f),s);
const swap=(s,a,b)=>{if(s.includes(b))return s;if(!s.includes(a))throw Error('khong thay: '+a.slice(0,80));return s.replace(a,()=>b);};
let w=rd('Services/DiscoveryIndexWorker.cs');
w=swap(w,`    entry.ValidFrom=from;entry.ValidTo=to;
   }
   await db.SaveChangesAsync();`,`    entry.ValidFrom=from;entry.ValidTo=to;
   }
   // Anh dich vu do nha cung cap tai: dung anh dau tien; go anh cu khi nha cung cap xoa het anh.
   foreach(var s in publicServices)
   {
    var entry=db.ChangeTracker.Entries<DiscoveryEntry>().Select(x=>x.Entity).FirstOrDefault(x=>x.ServiceId==s.MaDichVu);if(entry==null)continue;
    SetOwnPhoto(entry,Ui.ServicePhotos(s.AnhJson).Length>0?"/service-photos/"+s.MaDichVu+"/0":null,"/service-photos/");
   }
   await db.SaveChangesAsync();`);
w=swap(w,`    await db.SaveChangesAsync();
    var features=await db.Set<DiscoveryFeature>()`,`    SetOwnPhoto(e,Ui.ServicePhotos(b.PhotosJson).Length>0?"/co-so/"+b.Id+"/anh/0":null,"/co-so/");
    await db.SaveChangesAsync();
    var features=await db.Set<DiscoveryFeature>()`);
w=swap(w,` protected override async Task ExecuteAsync(`,` /// <summary>Anh nha cung cap uu tien hon anh minh hoa; khi khong con anh thi chi go anh do chinh nguon nay dat.</summary>
 static void SetOwnPhoto(DiscoveryEntry e,string? own,string prefix)
 {
  if(own!=null){if(e.ImagePath!=own){e.ImagePath=own;e.ImageCredit=null;e.ImageSourceUrl=null;}}
  else if(e.ImagePath!=null&&e.ImagePath.StartsWith(prefix,StringComparison.Ordinal)){e.ImagePath=null;e.ImageCredit=null;e.ImageSourceUrl=null;}
 }
 protected override async Task ExecuteAsync(`);
wr('Services/DiscoveryIndexWorker.cs',w);
console.log('ok');
