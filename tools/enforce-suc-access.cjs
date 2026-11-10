const fs=require('fs');const root='D:/KLTN/src/HoTroDuLich.Web';
function edit(p,from,to){let s=fs.readFileSync(root+'/'+p,'utf8');if(!s.includes(from))throw Error('Missing edit in '+p+': '+from.slice(0,70));fs.writeFileSync(root+'/'+p,s.replace(from,to));}
const classes={Plans:'TripPlan',Support:'Incident',Bookings:'Booking'};
for(const [name,suc]of Object.entries(classes))edit('Controllers/'+name+'Controller.cs','public class '+name+'Controller','[SucAuthorize(SucAccess.'+suc+')]\npublic class '+name+'Controller');
const maps={
 Companions:{Mine:'CompanionPost',Edit:'CompanionPost',Stop:'CompanionPost',Connect:'CompanionConnection',Connections:'CompanionConnection',Respond:'CompanionConnection',Agreement:'CompanionAgreement',Propose:'CompanionAgreement',AgreementAction:'CompanionAgreement'},
 Provider:{Index:'ServiceManagement',Edit:'ServiceManagement',Reception:'ServiceManagement',Verification:'ProviderVerification',Supplement:'ProviderVerification',Reapply:'ProviderVerification',Explain:'ProviderRights'},
 Admin:{Review:'ServiceReview',Verification:'ProviderVerification',Verify:'ProviderVerification',Provider:'ProviderRights',Rights:'ProviderRights',Explanation:'ProviderRights'},
 Bookings:{Edit:'BookingChange',Change:'BookingChange',ChangeAction:'BookingChange',Result:'ServiceResult',ResultAction:'ServiceResult'}
};
for(const [controller,actions]of Object.entries(maps)){
 const p=root+'/Controllers/'+controller+'Controller.cs';let s=fs.readFileSync(p,'utf8');
 for(const [action,suc]of Object.entries(actions)){
  const expression=new RegExp('(^[ \\t]*)([^\\n]*public (?:async Task<IActionResult>|IActionResult) '+action+'\\()','gm');
  s=s.replace(expression,(_,space,line)=>space+'[SucAuthorize(SucAccess.'+suc+')]\n'+space+line);
 }
 fs.writeFileSync(p,s);
}
edit('Services/AccountService.cs','public async Task SaveProfile(ProfileInput i){','public async Task SaveProfile(ProfileInput i){\n  RequireUseCase(SucAccess.Profile);');
edit('Services/TravelService.cs','public async Task<(List<DichVuDuLich> Services,List<DiaDiemDuLich> Places)> Explore(string? q,string? region,string? type,decimal? min,decimal? max){','public async Task<(List<DichVuDuLich> Services,List<DiaDiemDuLich> Places)> Explore(string? q,string? region,string? type,decimal? min,decimal? max){\n  RequireUseCase(SucAccess.TravelSearch);');
edit('Services/CompanionService.cs','public async Task<List<NhuCauDongHanh>> Needs(string? q,DateOnly? start,DateOnly? end,decimal? budget,string? style){','public async Task<List<NhuCauDongHanh>> Needs(string? q,DateOnly? start,DateOnly? end,decimal? budget,string? style){RequireUseCase(SucAccess.CompanionSearch);');
edit('Services/CompanionService.cs','public async Task<NhuCauDongHanh> Need(long id)=>await VisibleNeeds().SingleOrDefaultAsync(x=>x.MaNhuCau==id)??throw new BusinessException("Nhu cầu không còn công bố hoặc nằm ngoài phạm vi chia sẻ.",404);','public async Task<NhuCauDongHanh> Need(long id){RequireUseCase(SucAccess.CompanionSearch);return await VisibleNeeds().SingleOrDefaultAsync(x=>x.MaNhuCau==id)??throw new BusinessException("Nhu cầu không còn công bố hoặc nằm ngoài phạm vi chia sẻ.",404);}');
edit('Services/BookingService.cs','if(i.Decision=="Rut"){Require','if(i.Decision=="Rut"){RoleRequired(Traveler);Require');
edit('Services/BookingService.cs','else if(i.Decision=="KhachDongY"){Require','else if(i.Decision=="KhachDongY"){RoleRequired(Traveler);Require');
// The same decision string is used in two SUCs. Both customer branches require ACT01.
edit('Services/BookingService.cs','else if(i.Decision=="KhachDongY"){Require','else if(i.Decision=="KhachDongY"){RoleRequired(Traveler);Require');
edit('Services/BookingService.cs','if(i.Decision=="DaRut"){Require','if(i.Decision=="DaRut"){RoleRequired(Traveler);Require');
edit('Services/BookingService.cs','else{Require(p.MaTaiKhoan==UserId,"Chỉ nhà cung cấp sở hữu được phản hồi.",403);','else{RoleRequired(ProviderRole);Require(p.MaTaiKhoan==UserId,"Chỉ nhà cung cấp sở hữu được phản hồi.",403);');
edit('Services/BookingService.cs','if(i.Decision=="PhanAnh"){Require','if(i.Decision=="PhanAnh"){RoleRequired(Traveler);Require');
