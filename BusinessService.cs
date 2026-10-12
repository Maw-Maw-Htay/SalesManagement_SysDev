using Microsoft.EntityFrameworkCore;

namespace SalesManagement_SysDev;

public sealed class BusinessService
{
    public static MEmployee? Login(int id, string password)
    {
        using var db = new SalesManagementContext();
        return db.MEmployees.Include(x => x.Po).Include(x => x.So)
            .FirstOrDefault(x => x.EmId == id && x.EmPassword == password && x.EmFlag == 0 && x.Po.PoFlag == 0);
    }

    public static bool ChangePassword(int id, string oldPassword, string newPassword, out string message)
    {
        message = "";
        if (newPassword.Length < 4 || newPassword.Length > 10 || !newPassword.All(char.IsLetterOrDigit)) { message = "パスワードは半角英数字4～10文字です。"; return false; }
        using var db = new SalesManagementContext();
        var e = db.MEmployees.Find(id);
        if (e == null || e.EmPassword != oldPassword) { message = "現在のパスワードが正しくありません。"; return false; }
        e.EmPassword = newPassword; db.SaveChanges(); message = "パスワードを変更しました。"; return true;
    }

    public static void EnsureStock(MProduct product, SalesManagementContext db)
    {
        if (!db.TStocks.Any(x => x.PrId == product.PrId)) db.TStocks.Add(new TStock { PrId = product.PrId, StQuantity = 0, StFlag = 0 });
    }

    public static bool ConfirmOrder(int orderId, int employeeId, out string message)
    {
        message = ""; using var db = new SalesManagementContext(); using var tx = db.Database.BeginTransaction();
        try {
            var o = db.TOrders.Include(x => x.TOrderDetails).FirstOrDefault(x => x.OrId == orderId && x.OrFlag == 0);
            if (o == null || o.OrStateFlag == 1) { message = "対象の受注は確定済み、または存在しません。"; return false; }
            o.OrStateFlag = 1;
            var c = new TChumon { SoId=o.SoId, ClId=o.ClId, OrId=o.OrId, EmId=null, ChDate=DateTime.Today, ChStateFlag=0, ChFlag=0 };
            db.TChumons.Add(c); db.SaveChanges();
            foreach (var d in o.TOrderDetails) db.TChumonDetails.Add(new TChumonDetail { ChId=c.ChId, PrId=d.PrId, ChQuantity=d.OrQuantity });
            db.SaveChanges(); tx.Commit(); message = "受注を確定し、注文を作成しました。"; return true;
        } catch (Exception ex) { tx.Rollback(); message = "確定に失敗しました: " + ex.Message; return false; }
    }

    public static bool ConfirmChumon(int id, out string message)
    {
        message = ""; using var db = new SalesManagementContext(); using var tx = db.Database.BeginTransaction();
        try { var c=db.TChumons.Include(x=>x.TChumonDetails).FirstOrDefault(x=>x.ChId==id&&x.ChFlag==0); if(c==null||c.ChStateFlag==1){message="対象の注文は確定済み、または存在しません。";return false;} foreach(var d in c.TChumonDetails){var stock=db.TStocks.FirstOrDefault(x=>x.PrId==d.PrId&&x.StFlag==0);if(stock==null||stock.StQuantity<d.ChQuantity){message="在庫が不足しているため確定できません。";return false;}stock.StQuantity-=d.ChQuantity;} c.ChStateFlag=1; var s=new TSyukko{ClId=c.ClId,SoId=c.SoId,OrId=c.OrId,SyStateFlag=0,SyFlag=0};db.TSyukkos.Add(s);db.SaveChanges();foreach(var d in c.TChumonDetails)db.TSyukkoDetails.Add(new TSyukkoDetail{SyId=s.SyId,PrId=d.PrId,SyQuantity=d.ChQuantity});db.SaveChanges();tx.Commit();message="注文を確定し、在庫を減算して出庫を作成しました。";return true;} catch(Exception ex){tx.Rollback();message="確定に失敗しました: "+ex.Message;return false;}
    }

    public static bool ConfirmSyukko(int id, int employeeId, out string message)
    {
        message=""; using var db=new SalesManagementContext(); using var tx=db.Database.BeginTransaction();
        try { var s=db.TSyukkos.Include(x=>x.TSyukkoDetails).FirstOrDefault(x=>x.SyId==id&&x.SyFlag==0); if(s==null||s.SyStateFlag==1){message="対象の出庫は確定済み、または存在しません。";return false;} s.SyStateFlag=1;s.SyDate=DateTime.Today;s.EmId=employeeId; var a=new TArrival{SoId=s.SoId,ClId=s.ClId,OrId=s.OrId,ArDate=null,ArStateFlag=0,ArFlag=0};db.TArrivals.Add(a);db.SaveChanges();foreach(var d in s.TSyukkoDetails)db.TArrivalDetails.Add(new TArrivalDetail{ArId=a.ArId,PrId=d.PrId,ArQuantity=d.SyQuantity});db.SaveChanges();tx.Commit();message="出庫を確定し、入荷を作成しました。";return true;} catch(Exception ex){tx.Rollback();message="確定に失敗しました: "+ex.Message;return false;}
    }

    public static bool ConfirmArrival(int id, int employeeId, out string message)
    {
        message=""; using var db=new SalesManagementContext(); using var tx=db.Database.BeginTransaction();
        try { var a=db.TArrivals.Include(x=>x.TArrivalDetails).FirstOrDefault(x=>x.ArId==id&&x.ArFlag==0); if(a==null||a.ArStateFlag==1){message="対象の入荷は確定済み、または存在しません。";return false;}a.ArStateFlag=1;a.ArDate=DateTime.Today;a.EmId=employeeId;var sh=new TShipment{ClId=a.ClId,SoId=a.SoId,OrId=a.OrId,ShStateFlag=0,ShFlag=0};db.TShipments.Add(sh);db.SaveChanges();foreach(var d in a.TArrivalDetails){if(!d.PrId.HasValue||!d.ArQuantity.HasValue)continue;db.TShipmentDetails.Add(new TShipmentDetail{ShId=sh.ShId,PrId=d.PrId.Value,ShQuantity=d.ArQuantity.Value});}db.SaveChanges();tx.Commit();message="入荷を確定し、出荷を作成しました。";return true;}catch(Exception ex){tx.Rollback();message="確定に失敗しました: "+ex.Message;return false;}
    }

    public static bool ConfirmShipment(int id, int employeeId, out string message)
    {
        message=""; using var db=new SalesManagementContext(); using var tx=db.Database.BeginTransaction();
        try { var sh=db.TShipments.Include(x=>x.TShipmentDetails).FirstOrDefault(x=>x.ShId==id&&x.ShFlag==0);if(sh==null||sh.ShStateFlag==1){message="対象の出荷は確定済み、または存在しません。";return false;}sh.ShStateFlag=1;sh.ShFinishDate=DateTime.Today;sh.EmId=employeeId;var sale=new TSale{ClId=sh.ClId,SoId=sh.SoId,OrId=sh.OrId,EmId=employeeId,SaDate=DateTime.Today,SaFlag=0};db.TSales.Add(sale);db.SaveChanges();foreach(var d in sh.TShipmentDetails){var p=db.MProducts.Find(d.PrId);db.TSaleDetails.Add(new TSaleDetail{SaId=sale.SaId,PrId=d.PrId,SaQuantity=d.ShQuantity,SaPrTotalPrice=(p?.Price??0)*d.ShQuantity});}db.SaveChanges();tx.Commit();message="出荷を確定し、売上を作成しました。";return true;}catch(Exception ex){tx.Rollback();message="確定に失敗しました: "+ex.Message;return false;}
    }
}
