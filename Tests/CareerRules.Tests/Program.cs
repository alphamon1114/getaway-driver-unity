using Getaway;
using System.Text.Json;

int count = 0;
var results = new List<string>();
void Check(bool ok, string name) { if (!ok) throw new Exception(name); results.Add("PASS " + name); Console.WriteLine("PASS " + name); count++; }
// Deadline 14s, full marks at or under 7s. Arriving sooner must never be worth less,
// otherwise the garage would be selling engine upgrades that cost the player score.
PickupSchedule At(double time)
{
    var a = new PickupSchedule(14, 7, 1000);
    a.Advance(time - 0.02, false); a.Advance(0.02, true); return a;
}
bool Near(int value, int expected, int slack = 2) => Math.Abs(value - expected) <= slack;
Check(Near(At(7).Score, 1000), "Arriving at par time pays the full score");
Check(At(4).Score == 1000 && At(0.5).Score == 1000, "Beating par cannot pay less than par");
Check(Near(At(10.5).Score, 500), "Score falls linearly between par and the deadline");
Check(PickupSchedule.ScoreFor(14, 14, 7, 1000) == 0, "Arriving on the deadline scores nothing");
Check(PickupSchedule.ScoreFor(20, 14, 7, 1000) == 0, "Arriving past the deadline scores nothing");
Check(PickupSchedule.ScoreFor(5, 6, 9, 1000) == 1000, "A par slower than the deadline still pays out");
int monotone = int.MaxValue; bool falling = true;
for (double t = 0.5; t <= 14; t += 0.25)
{
    int s = PickupSchedule.ScoreFor(t, 14, 7, 1000);
    if (s > monotone) falling = false;
    monotone = s;
}
Check(falling, "Score never rewards a slower run");

var board = At(5); int lockedScore = board.Score;
board.Advance(1.0, true); Check(!board.Boarded, "One second at the bank is not enough to board");
board.Advance(0.5, false); Check(board.Boarding == 0, "Moving off resets boarding progress");
board.Advance(2.01, true); Check(board.Boarded && !board.Missed, "Two continuous seconds boards the crew");
Check(board.Score == lockedScore, "Boarding does not disturb the locked arrival score");

var reenter = At(9); int firstStop = reenter.Score;
reenter.Advance(1, false); reenter.Advance(0.02, true);
Check(reenter.Score == firstStop, "Re-entering the bank cannot reroll the arrival score");

var miss = new PickupSchedule(14, 7, 1000); miss.Advance(14.1, false);
Check(miss.Missed && !miss.Boarded && miss.Score == 0, "Never reaching the bank fails with no score");

var late = At(13);
for (int i = 0; i < 200 && !late.Boarded && !late.Missed; i++) late.Advance(0.02, true);
Check(late.Missed && !late.Boarded, "Arriving too late to finish boarding fails");

var justInTime = At(11.5);
for (int i = 0; i < 200 && !justInTime.Boarded && !justInTime.Missed; i++) justInTime.Advance(0.02, true);
Check(justInTime.Boarded && !justInTime.Missed, "Arriving with two seconds to spare boards in time");
Check(justInTime.Remaining > 0 && justInTime.Remaining < 1, "Remaining time counts down against the deadline");
Check(GarageCatalog.CashLoss(12000, 10, 120, 0) == 1200, "Police damage deducts proportional cash");
Check(GarageCatalog.CashLoss(12000, 20, 120, 0) == 2400, "Double damage causes double loss");
Check(GarageCatalog.CashLoss(12000, 10, 120, 3) == 480, "Safe level three reduces money loss by 60 percent");
Check(GarageCatalog.CashLoss(100, 30, 120, 0) == 100, "Cash never becomes negative");

var legacy = new CareerData { schemaVersion = 0, highestUnlocked = 2, completedRuns = 5, vehicles = null, settledRuns = null, selectedVehicle = null };
GarageCatalog.Normalize(legacy);
Check(legacy.highestUnlocked == 2 && legacy.completedRuns == 5 && legacy.wallet == 0 && legacy.selectedVehicle == "sedan", "Legacy progress migrates with starter car and zero wallet");
var account = new CareerAccount(legacy, _ => true);
Check(!account.BuyVehicle("coupe", out _) && account.Data.wallet == 0, "Insufficient balance cannot buy a vehicle");
Check(!account.SelectVehicle("van", out _), "Unowned vehicle cannot be equipped");
Check(account.Settle("run-one", 30000, 850, 2, out _) && account.Data.wallet == 30000, "Successful run credits wallet");
account.Settle("run-one", 30000, 850, 2, out _);
Check(account.Data.wallet == 30000 && account.Data.completedRuns == 6 && account.Data.totalArrivalScore == 850, "Repeated settlement is idempotent");
Check(account.BuyVehicle("coupe", out _) && account.Data.wallet == 12000, "Vehicle purchase charges once");
Check(!account.BuyVehicle("coupe", out _) && account.Data.wallet == 12000, "Duplicate ownership cannot charge again");
account.SelectVehicle("coupe", out _); account.BuyUpgrade(UpgradeKind.Engine, out _);
Check(account.Selected.engine == 1 && account.Data.wallet == 9000, "Engine upgrade is purchased and saved");
account.SelectVehicle("sedan", out _);
Check(account.Selected.engine == 0, "Upgrade levels are per vehicle");
account.Settle("run-two", 100000, 1000, 2, out _);
for (int i = 0; i < 3; i++) account.BuyUpgrade(UpgradeKind.Armor, out _);
long balance = account.Data.wallet;
Check(!account.BuyUpgrade(UpgradeKind.Armor, out _) && account.Data.wallet == balance, "Max upgrade cannot charge again");
var broken = new CareerAccount(account.Data.Copy(), _ => false);
long before = broken.Data.wallet;
Check(!broken.BuyVehicle("van", out _) && broken.Data.wallet == before && !broken.Data.vehicles.Exists(v => v.id == "van"), "Failed purchase save rolls back both currency and ownership");
Check(!broken.BuyUpgrade(UpgradeKind.Safe, out _) && broken.Selected.safe == 0, "Failed upgrade save does not mutate levels");
Check(!broken.SelectVehicle("coupe", out _) && broken.Data.selectedVehicle == "sedan", "Failed equipment save keeps previous selection");
bool saveWorks = false;
var retry = new CareerAccount(new CareerData(), _ => saveWorks);
Check(!retry.Settle("retry", 12000, 1000, 1, out _) && retry.Data.wallet == 0, "Failed payout remains unpaid");
saveWorks = true; retry.Settle("retry", 12000, 1000, 1, out _); retry.Settle("retry", 12000, 1000, 1, out _);
Check(retry.Data.wallet == 12000 && retry.Data.completedRuns == 1, "Retry pays exactly once");

string folder = Path.GetFullPath(args.Length > 0 ? args[0] : Path.Combine("Logs", "career-tests", Guid.NewGuid().ToString("N")));
Directory.CreateDirectory(folder);
var jsonOptions = new JsonSerializerOptions { IncludeFields = true };
string Encode(CareerData d) => JsonSerializer.Serialize(d, jsonOptions);
CareerData Decode(string s) => JsonSerializer.Deserialize<CareerData>(s, jsonOptions);
string file = Path.Combine(folder, "progress.json");
File.WriteAllText(file, "{\"highestUnlocked\":2,\"completedRuns\":5}");
var storage = new CareerStorage(file, Encode, Decode);
var loaded = storage.Load(); Check(loaded.highestUnlocked == 2 && loaded.vehicles.Count == 1, "Legacy JSON loads through production storage");
var persistent = new CareerAccount(loaded, storage.Save);
Check(persistent.Settle("durable", 50000, 900, 2, out _), "Persist earnings: " + storage.Status);
Check(persistent.BuyVehicle("coupe", out _), "Persist purchase: " + storage.Status);
Check(persistent.SelectVehicle("coupe", out _), "Persist selection: " + storage.Status);
Check(persistent.BuyUpgrade(UpgradeKind.Tires, out _), "Persist upgrade: " + storage.Status);
var reloaded = new CareerStorage(file, Encode, Decode).Load();
Check(reloaded.wallet == 30000 && reloaded.selectedVehicle == "coupe" && reloaded.vehicles.Find(v => v.id == "coupe").tires == 1, "Restart restores wallet, purchased car, selection and upgrade");
long savedBalance = reloaded.wallet;
var again = new CareerAccount(reloaded, storage.Save); again.Settle("durable", 50000, 900, 2, out _);
Check(again.Data.wallet == savedBalance, "Idempotent payout survives restart");
var backupData = Decode(File.ReadAllText(file + ".bak"));
File.WriteAllText(file, "truncated");
var recovery = new CareerStorage(file, Encode, Decode); var recovered = recovery.Load();
Check(!recovery.ReadOnly && recovered.wallet == backupData.wallet && Directory.GetFiles(folder, "*.corrupt-*").Length == 1, "Corrupt primary recovers backup and preserves evidence");
Check(recovery.Save(recovered) && Decode(File.ReadAllText(file + ".bak")).wallet == backupData.wallet, "Repair does not replace good backup with corrupt data");
File.WriteAllText(file, "broken"); File.WriteAllText(file + ".bak", "broken too");
var blocked = new CareerStorage(file, Encode, Decode); blocked.Load();
Check(blocked.ReadOnly && !blocked.Save(new CareerData()) && File.ReadAllText(file) == "broken", "Two corrupt saves block writes rather than silently reset money");
File.WriteAllText(file, "{\"schemaVersion\":99,\"highestUnlocked\":1,\"completedRuns\":1}");
var future = new CareerStorage(file, Encode, Decode); future.Load();
Check(future.ReadOnly && !future.Save(new CareerData()), "Future save versions cannot be downgraded");
string conflict = Path.Combine(folder, "not-a-directory"); File.WriteAllText(conflict, "block");
var failedIO = new CareerStorage(Path.Combine(conflict, "progress.json"), Encode, Decode);
Check(!failedIO.Save(account.Data), "Real filesystem write failure is reported");
Console.WriteLine($"{count} checks passed. Production career rules and IO; not a Unity physics/play-mode test.");
if (args.Length > 1)
{
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1])));
    File.WriteAllText(args[1], DateTime.UtcNow.ToString("O") + "\n" + string.Join("\n", results) + $"\n{count} checks passed. Production career rules and IO; not Unity Play Mode.\n");
}
