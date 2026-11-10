const fs=require('fs');const p='D:/KLTN/src/HoTroDuLich.Web/Services/BookingService.cs';let s=fs.readFileSync(p,'utf8');
s=s.replace('public async Task BookingAction(CommandInput i){await Execute','public async Task BookingAction(CommandInput i){RoleRequired(new[]{"Rut","KhachDongY"}.Contains(i.Decision)?Traveler:ProviderRole);await Execute');
s=s.replace('public async Task ChangeAction(CommandInput i){await Execute','public async Task ChangeAction(CommandInput i){RoleRequired(new[]{"DaRut","KhachDongY"}.Contains(i.Decision)?Traveler:ProviderRole);await Execute');
s=s.replace('public async Task ResultAction(CommandInput i){await Execute','public async Task ResultAction(CommandInput i){RoleRequired(i.Decision=="PhanAnh"?Traveler:ProviderRole);await Execute');
fs.writeFileSync(p,s);
