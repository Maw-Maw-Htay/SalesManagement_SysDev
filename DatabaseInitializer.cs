using Microsoft.EntityFrameworkCore;

namespace SalesManagement_SysDev;

/// <summary>
/// 既存の SalesManagement データベースを壊さずに起動可能にする初期化処理。
/// 既存データは再作成せず、足りないマスタとサンプルアカウントだけを追加する。
/// </summary>
public static class DatabaseInitializer
{
    public static void EnsureReady()
    {
        using var db = new SalesManagementContext();
        db.Database.EnsureCreated();
        SeedMinimumData(db);
    }

    public static void SeedMinimumData(SalesManagementContext db)
    {
        using var transaction = db.Database.BeginTransaction();
        try
        {
            var positions = new Dictionary<string, MPosition>
            {
                ["管理者"] = GetOrCreate(db.MPositions, x => x.PoName == "管理者", () => new MPosition { PoName = "管理者", PoFlag = 0 }),
                ["営業"] = GetOrCreate(db.MPositions, x => x.PoName == "営業", () => new MPosition { PoName = "営業", PoFlag = 0 }),
                ["物流"] = GetOrCreate(db.MPositions, x => x.PoName == "物流", () => new MPosition { PoName = "物流", PoFlag = 0 })
            };
            db.SaveChanges();

            var offices = new[]
            {
                GetOrCreate(db.MSalesOffices, x => x.SoName == "北大阪営業所", () => new MSalesOffice { SoName = "北大阪営業所", SoAddress = "Regional City X", SoPhone = "000-0000-0000", SoPostal = "0000000", SoFax = "000-0000-0000", SoFlag = 0 }),
                GetOrCreate(db.MSalesOffices, x => x.SoName == "兵庫営業所", () => new MSalesOffice { SoName = "兵庫営業所", SoAddress = "Regional City X", SoPhone = "000-0000-0000", SoPostal = "0000000", SoFax = "000-0000-0000", SoFlag = 0 }),
                GetOrCreate(db.MSalesOffices, x => x.SoName == "鹿営業所", () => new MSalesOffice { SoName = "鹿営業所", SoAddress = "Regional City X", SoPhone = "000-0000-0000", SoPostal = "0000000", SoFax = "000-0000-0000", SoFlag = 0 }),
                GetOrCreate(db.MSalesOffices, x => x.SoName == "京都営業所", () => new MSalesOffice { SoName = "京都営業所", SoAddress = "Regional City X", SoPhone = "000-0000-0000", SoPostal = "0000000", SoFax = "000-0000-0000", SoFlag = 0 }),
                GetOrCreate(db.MSalesOffices, x => x.SoName == "和歌山営業所", () => new MSalesOffice { SoName = "和歌山営業所", SoAddress = "Regional City X", SoPhone = "000-0000-0000", SoPostal = "0000000", SoFax = "000-0000-0000", SoFlag = 0 })
            };
            db.SaveChanges();

            EnsureEmployee(db, 1227, "生田徳次郎", "1227", offices[0], positions["管理者"]);
            EnsureEmployee(db, 310, "高谷春男", "0310", offices[0], positions["営業"]);
            EnsureEmployee(db, 116, "坂口郁美", "0116", offices[1], positions["物流"]);

            var maker = GetOrCreate(db.MMakers, x => x.MaName == "Regional Tech", () => new MMaker { MaName = "Regional Tech", MaAddress = "Regional City X", MaPhone = "000-0000-0000", MaPostal = "0000000", MaFax = "000-0000-0000", MaFlag = 0 });
            var major = GetOrCreate(db.MMajorClassifications, x => x.McName == "電子機器", () => new MMajorClassification { McName = "電子機器", McFlag = 0 });
            db.SaveChanges();
            var small = GetOrCreate(db.MSmallClassifications, x => x.ScName == "パソコン", () => new MSmallClassification { McId = major.McId, ScName = "パソコン", ScFlag = 0 });
            db.SaveChanges();

            EnsureProduct(db, maker, small, "PC-001", "業務用ノートPC", "490000000001", 82000, 10, 50);
            EnsureProduct(db, maker, small, "PC-002", "業務用デスクトップPC", "490000000002", 91000, 8, 30);
            db.SaveChanges();

            EnsureClient(db, "C-001", "Regional City X 電機商会", offices[0]);
            EnsureClient(db, "C-002", "Xネットワーク販売", offices[1]);
            db.SaveChanges();

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// 画面確認・検索テスト用に、サンプル系列を各業務テーブルへ最大150件作成する。
    /// 再実行時は SAMPLE150 識別子を使って重複を防止する。
    /// </summary>
    public static void Seed150SampleData()
    {
        using var db = new SalesManagementContext();
        db.Database.EnsureCreated();
        SeedMinimumData(db);

        using var transaction = db.Database.BeginTransaction();
        try
        {
            var offices = db.MSalesOffices.Where(x => x.SoFlag == 0).OrderBy(x => x.SoId).ToList();
            var positions = db.MPositions.Where(x => x.PoFlag == 0).OrderBy(x => x.PoId).ToList();
            var makerList = db.MMakers.Where(x => x.MaFlag == 0).ToList();
            var majorList = db.MMajorClassifications.Where(x => x.McFlag == 0).ToList();

            for (var i = 1; i <= 150; i++)
            {
                var name = $"サンプルメーカー{i:000}";
                if (!db.MMakers.Any(x => x.MaName == name)) db.MMakers.Add(new MMaker { MaName = name, MaAddress = "Regional City X", MaPhone = $"050-150-{i:0000}", MaPostal = "0000000", MaFax = "050-150-0000", MaFlag = 0 });
            }
            for (var i = 1; i <= 150; i++)
            {
                var name = $"サンプル大分類{i:000}";
                if (!db.MMajorClassifications.Any(x => x.McName == name)) db.MMajorClassifications.Add(new MMajorClassification { McName = name, McFlag = 0 });
            }
            db.SaveChanges();
            makerList = db.MMakers.Where(x => x.MaFlag == 0).OrderBy(x => x.MaId).ToList();
            majorList = db.MMajorClassifications.Where(x => x.McFlag == 0).OrderBy(x => x.McId).ToList();

            for (var i = 1; i <= 150; i++)
            {
                var name = $"サンプル小分類{i:000}";
                if (!db.MSmallClassifications.Any(x => x.ScName == name)) db.MSmallClassifications.Add(new MSmallClassification { McId = majorList[(i - 1) % majorList.Count].McId, ScName = name, ScFlag = 0 });
            }
            db.SaveChanges();

            for (var i = 1; i <= 150; i++)
            {
                var id = 2000 + i;
                if (!db.MEmployees.Any(x => x.EmId == id)) db.MEmployees.Add(new MEmployee { EmId = id, EmName = $"サンプル社員{i:000}", SoId = offices[(i - 1) % offices.Count].SoId, PoId = positions[(i - 1) % positions.Count].PoId, EmHiredate = DateTime.Today.AddDays(-i), EmPassword = $"{id}", EmPhone = $"050-200-{i:0000}", EmFlag = 0 });
            }
            for (var i = 1; i <= 150; i++)
            {
                var name = $"サンプル顧客{i:000}";
                if (!db.MClients.Any(x => x.ClName == name)) db.MClients.Add(new MClient { SoId = offices[(i - 1) % offices.Count].SoId, ClName = name, ClAddress = "Regional City X", ClPhone = $"050-300-{i:0000}", ClPostal = "0000000", ClFax = "050-300-0000", ClFlag = 0 });
            }
            db.SaveChanges();

            var smallList = db.MSmallClassifications.Where(x => x.ScFlag == 0).OrderBy(x => x.ScId).ToList();
            var employeeList = db.MEmployees.Where(x => x.EmFlag == 0).OrderBy(x => x.EmId).ToList();
            for (var i = 1; i <= 150; i++)
            {
                var barcode = $"490150{i:000000}";
                if (!db.MProducts.Any(x => x.PrJcode == barcode)) db.MProducts.Add(new MProduct { MaId = makerList[(i - 1) % makerList.Count].MaId, ScId = smallList[(i - 1) % smallList.Count].ScId, PrJcode = barcode, PrName = $"サンプル電子機器{i:000}", PrModelNumber = $"SAMPLE-{i:000}", PrColor = i % 2 == 0 ? "黒" : "白", Price = 3000 + i * 100, PrSafetyStock = 10 + i % 20, PrReleaseDate = DateTime.Today.AddDays(-i), PrFlag = 0 });
            }
            db.SaveChanges();

            var productList = db.MProducts.Where(x => x.PrFlag == 0).OrderBy(x => x.PrId).ToList();
            for (var i = 1; i <= 150; i++)
            {
                var product = productList.FirstOrDefault(x => x.PrJcode == $"490150{i:000000}");
                if (product != null && !db.TStocks.Any(x => x.PrId == product.PrId)) db.TStocks.Add(new TStock { PrId = product.PrId, StQuantity = 20 + i % 80, StFlag = 0 });
            }
            db.SaveChanges();

            var sampleClients = db.MClients.Where(x => x.ClName.StartsWith("サンプル顧客")).OrderBy(x => x.ClId).ToList();
            var sampleProducts = db.MProducts.Where(x => x.PrName.StartsWith("サンプル電子機器")).OrderBy(x => x.PrId).ToList();
            var sampleEmployees = db.MEmployees.Where(x => x.EmName.StartsWith("サンプル社員")).OrderBy(x => x.EmId).ToList();
            var sampleOrders = new List<TOrder>();
            for (var i = 1; i <= 150; i++)
            {
                var marker = $"SAMPLE150-{i:000}";
                var order = db.TOrders.FirstOrDefault(x => x.ClCharge == marker);
                if (order == null)
                {
                    var employee = sampleEmployees[(i - 1) % sampleEmployees.Count];
                    var customer = sampleClients[(i - 1) % sampleClients.Count];
                    order = new TOrder { SoId = employee.SoId, EmId = employee.EmId, ClId = customer.ClId, ClCharge = marker, OrDate = DateTime.Today.AddDays(-i), OrStateFlag = i % 3 == 0 ? 1 : 0, OrFlag = i % 20 == 0 ? 2 : 0, OrHidden = i % 20 == 0 ? "SAMPLE150テスト非表示" : null };
                    db.TOrders.Add(order);
                    db.SaveChanges();
                }
                sampleOrders.Add(order);
                var product = sampleProducts[(i - 1) % sampleProducts.Count];
                if (!db.TOrderDetails.Any(x => x.OrId == order.OrId)) db.TOrderDetails.Add(new TOrderDetail { OrId = order.OrId, PrId = product.PrId, OrQuantity = 1 + i % 5, OrTotalPrice = product.Price * (1 + i % 5) });
            }
            db.SaveChanges();

            for (var i = 0; i < sampleOrders.Count; i++)
            {
                var order = sampleOrders[i];
                var product = sampleProducts[i % sampleProducts.Count];
                var employee = sampleEmployees[i % sampleEmployees.Count];
                if (!db.TChumons.Any(x => x.OrId == order.OrId))
                {
                    var row = new TChumon { SoId = order.SoId, EmId = employee.EmId, ClId = order.ClId, OrId = order.OrId, ChDate = order.OrDate.AddDays(1), ChStateFlag = i % 2, ChFlag = 0 };
                    db.TChumons.Add(row); db.SaveChanges(); db.TChumonDetails.Add(new TChumonDetail { ChId = row.ChId, PrId = product.PrId, ChQuantity = 1 + i % 5 });
                }
                if (!db.TSyukkos.Any(x => x.OrId == order.OrId))
                {
                    var row = new TSyukko { EmId = employee.EmId, ClId = order.ClId, SoId = order.SoId, OrId = order.OrId, SyDate = order.OrDate.AddDays(2), SyStateFlag = i % 2, SyFlag = 0 };
                    db.TSyukkos.Add(row); db.SaveChanges(); db.TSyukkoDetails.Add(new TSyukkoDetail { SyId = row.SyId, PrId = product.PrId, SyQuantity = 1 + i % 5 });
                }
                if (!db.TArrivals.Any(x => x.OrId == order.OrId))
                {
                    var row = new TArrival { SoId = order.SoId, EmId = employee.EmId, ClId = order.ClId, OrId = order.OrId, ArDate = order.OrDate.AddDays(3), ArStateFlag = i % 2, ArFlag = 0 };
                    db.TArrivals.Add(row); db.SaveChanges(); db.TArrivalDetails.Add(new TArrivalDetail { ArId = row.ArId, PrId = product.PrId, ArQuantity = 1 + i % 5 });
                }
                if (!db.TShipments.Any(x => x.OrId == order.OrId))
                {
                    var row = new TShipment { ClId = order.ClId, EmId = employee.EmId, SoId = order.SoId, OrId = order.OrId, ShStateFlag = i % 2, ShFinishDate = order.OrDate.AddDays(4), ShFlag = 0 };
                    db.TShipments.Add(row); db.SaveChanges(); db.TShipmentDetails.Add(new TShipmentDetail { ShId = row.ShId, PrId = product.PrId, ShQuantity = 1 + i % 5 });
                }
                if (!db.TSales.Any(x => x.OrId == order.OrId))
                {
                    var row = new TSale { ClId = order.ClId, SoId = order.SoId, EmId = employee.EmId, OrId = order.OrId, SaDate = order.OrDate.AddDays(5), SaFlag = 0 };
                    db.TSales.Add(row); db.SaveChanges(); db.TSaleDetails.Add(new TSaleDetail { SaId = row.SaId, PrId = product.PrId, SaQuantity = 1 + i % 5, SaPrTotalPrice = product.Price * (1 + i % 5) });
                }
            }
            db.SaveChanges();

            for (var i = 1; i <= 150; i++)
            {
                var date = new DateTime(2090, 1, 1).AddDays(i - 1);
                if (!db.THattyus.Any(x => x.HaDate == date))
                {
                    var maker = makerList[(i - 1) % makerList.Count];
                    var employee = employeeList[(i - 1) % employeeList.Count];
                    var row = new THattyu { MaId = maker.MaId, EmId = employee.EmId, HaDate = date, WaWarehouseFlag = i % 2, HaFlag = 0 };
                    db.THattyus.Add(row); db.SaveChanges();
                    var product = productList[(i - 1) % productList.Count];
                    db.THattyuDetails.Add(new THattyuDetail { HaId = row.HaId, PrId = product.PrId, HaQuantity = 5 + i % 20 });
                    var warehousing = new TWarehousing { HaId = row.HaId, EmId = employee.EmId, WaDate = date.AddDays(1), WaShelfFlag = i % 2, WaFlag = 0 };
                    db.TWarehousings.Add(warehousing); db.SaveChanges();
                    db.TWarehousingDetails.Add(new TWarehousingDetail { WaId = warehousing.WaId, PrId = product.PrId, WaQuantity = 5 + i % 20 });
                }
            }
            db.SaveChanges();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static void EnsureEmployee(SalesManagementContext db, int id, string name, string password, MSalesOffice office, MPosition position)
    {
        if (db.MEmployees.Any(x => x.EmId == id)) return;
        db.MEmployees.Add(new MEmployee { EmId = id, EmName = name, EmPassword = password, EmPhone = "000-0000-0000", EmHiredate = DateTime.Today, EmFlag = 0, SoId = office.SoId, PoId = position.PoId });
    }

    private static void EnsureProduct(SalesManagementContext db, MMaker maker, MSmallClassification small, string code, string name, string barcode, decimal price, int safetyStock, int quantity)
    {
        var product = db.MProducts.FirstOrDefault(x => x.PrJcode == barcode || x.PrName == name);
        if (product == null)
        {
            product = new MProduct { MaId = maker.MaId, ScId = small.ScId, PrJcode = barcode, PrName = name, PrModelNumber = code, PrColor = "標準", Price = price, PrSafetyStock = safetyStock, PrReleaseDate = DateTime.Today, PrFlag = 0 };
            db.MProducts.Add(product);
            db.SaveChanges();
        }
        if (!db.TStocks.Any(x => x.PrId == product.PrId)) db.TStocks.Add(new TStock { PrId = product.PrId, StQuantity = quantity, StFlag = 0 });
    }

    private static void EnsureClient(SalesManagementContext db, string code, string name, MSalesOffice office)
    {
        if (db.MClients.Any(x => x.ClName == name)) return;
        db.MClients.Add(new MClient { ClName = name, ClAddress = "Regional City X", ClPhone = "000-0000-0000", ClPostal = "0000000", ClFax = "000-0000-0000", ClFlag = 0, SoId = office.SoId });
    }

    private static T GetOrCreate<T>(DbSet<T> set, Func<T, bool> predicate, Func<T> create) where T : class
    {
        var existing = set.Local.FirstOrDefault(predicate) ?? set.AsNoTracking().FirstOrDefault(predicate);
        if (existing != null) return existing;
        var entity = create();
        set.Add(entity);
        return entity;
    }
}
