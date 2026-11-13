using System.Collections.Concurrent;
using System.Threading.Channels;
namespace HoTroDuLich.Web.Services;

/// <summary>Phat su kien tin nhan tuc thi toi cac tab dang mo cua tung tai khoan (Server-Sent Events). Chay trong mot tien trinh web.</summary>
public sealed class ChatBroker
{
 readonly ConcurrentDictionary<long,ConcurrentDictionary<Guid,Channel<string>>> subs=new();
 public (Guid Id,ChannelReader<string> Reader) Subscribe(long account)
 {
  var channel=Channel.CreateBounded<string>(new BoundedChannelOptions(100){FullMode=BoundedChannelFullMode.DropOldest,SingleReader=true});
  var id=Guid.NewGuid();subs.GetOrAdd(account,_=>new()).TryAdd(id,channel);return (id,channel.Reader);
 }
 public void Unsubscribe(long account,Guid id){if(subs.TryGetValue(account,out var own)&&own.TryRemove(id,out var ch)){ch.Writer.TryComplete();if(own.IsEmpty)subs.TryRemove(account,out _);}}
 public void Publish(long account,string json){if(subs.TryGetValue(account,out var own))foreach(var ch in own.Values)ch.Writer.TryWrite(json);}
 public bool IsOnline(long account)=>subs.TryGetValue(account,out var own)&&!own.IsEmpty;
}
