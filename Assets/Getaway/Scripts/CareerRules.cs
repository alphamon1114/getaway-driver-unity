using System;
using System.Collections.Generic;

namespace Getaway
{
    // These production rules are also tested without an editor or a graphics device.
    // The crew waits inside the bank from the start: the job is to beat the police roadblocks
    // there before the deadline, and arriving sooner is always worth more.
    public sealed class PickupSchedule
    {
        public double Elapsed { get; private set; }
        public double ArrivalTime { get; private set; } = -1;
        public int Score { get; private set; }
        public double Boarding { get; private set; }
        public bool Boarded { get; private set; }
        public bool Missed { get; private set; }
        public readonly double Deadline, ParTime, BoardingSeconds;
        public readonly int MaxScore;
        public double Remaining => Math.Max(0, Deadline - Elapsed);
        public PickupSchedule(double deadline, double parTime, int maxScore, double boardingSeconds = 2)
        {
            Deadline = Math.Max(1, deadline);
            ParTime = Math.Max(0, Math.Min(parTime, Deadline - 0.1));
            MaxScore = Math.Max(0, maxScore);
            BoardingSeconds = Math.Max(0.1, boardingSeconds);
        }
        // Full marks at or under par, sliding to nothing at the deadline. Faster is never punished,
        // so engine upgrades buy score instead of costing it.
        public static int ScoreFor(double arrival, double deadline, double parTime, int maxScore)
        {
            if (arrival < 0 || arrival > deadline) return 0;
            if (deadline <= parTime) return maxScore;
            double fraction = (deadline - arrival) / (deadline - parTime);
            return (int)Math.Round(maxScore * Math.Max(0, Math.Min(1, fraction)), MidpointRounding.AwayFromZero);
        }
        public void Advance(double dt, bool stoppedAtBank)
        {
            if (Boarded || Missed || dt <= 0 || double.IsNaN(dt) || double.IsInfinity(dt)) return;
            Elapsed += dt;
            if (stoppedAtBank && ArrivalTime < 0)
            {
                ArrivalTime = Elapsed;
                Score = ScoreFor(ArrivalTime, Deadline, ParTime, MaxScore);
            }
            Boarding = stoppedAtBank ? Boarding + dt : 0;
            if (Boarding + 0.000001 >= BoardingSeconds) Boarded = true;
            else if (Elapsed >= Deadline) Missed = true;
        }
    }
    [Serializable]
    public sealed class OwnedVehicle
    {
        public string id;
        public int engine, tires, armor, safe;
        public OwnedVehicle Copy() => new OwnedVehicle { id = id, engine = engine, tires = tires, armor = armor, safe = safe };
    }
    [Serializable]
    public class CareerData
    {
        public int schemaVersion = 2;
        public int highestUnlocked, completedRuns;
        public long wallet, totalArrivalScore;
        public int bestArrivalScore;
        public string selectedVehicle = "sedan";
        public List<OwnedVehicle> vehicles = new List<OwnedVehicle>();
        public List<string> settledRuns = new List<string>();
        public CareerData Copy()
        {
            var copy = (CareerData)MemberwiseClone();
            copy.vehicles = vehicles.ConvertAll(v => v.Copy()); copy.settledRuns = new List<string>(settledRuns);
            return copy;
        }
    }
    public enum UpgradeKind { Engine, Tires, Armor, Safe }
    public sealed class VehicleSpec
    {
        public readonly string Id, Name;
        public readonly int Price;
        public readonly float Speed, Acceleration, Health, Grip;
        public VehicleSpec(string id, string name, int price, float speed, float acceleration, float health, float grip)
        { Id = id; Name = name; Price = price; Speed = speed; Acceleration = acceleration; Health = health; Grip = grip; }
    }
    public static class GarageCatalog
    {
        public const int MaxUpgrade = 3;
        public const long MaxMoney = 2000000000;
        public static readonly VehicleSpec[] Vehicles = {
            new VehicleSpec("sedan", "Street Sedan", 0, 36, 14, 100, 9),
            new VehicleSpec("coupe", "Night Runner", 18000, 42, 17, 90, 10),
            new VehicleSpec("van", "Armored Van", 24000, 32, 11, 160, 8)
        };
        public static VehicleSpec Find(string id) => Array.Find(Vehicles, v => v.Id == id);
        public static int Level(OwnedVehicle car, UpgradeKind kind)
        {
            switch (kind) { case UpgradeKind.Engine: return car.engine; case UpgradeKind.Tires: return car.tires; case UpgradeKind.Armor: return car.armor; default: return car.safe; }
        }
        public static int UpgradePrice(OwnedVehicle car, UpgradeKind kind)
        {
            int basePrice = kind == UpgradeKind.Engine ? 3000 : kind == UpgradeKind.Safe ? 2500 : 2000;
            return basePrice * (Level(car, kind) + 1);
        }
        public static string UpgradeDescription(UpgradeKind kind)
        {
            switch (kind) {
                case UpgradeKind.Engine: return "+8% top speed / +10% acceleration per level";
                case UpgradeKind.Tires: return "+10% grip / faster grip recovery per level";
                case UpgradeKind.Armor: return "-12% collision damage per level";
                default: return "-20% cash loss from police impacts per level";
            }
        }
        public static void Normalize(CareerData data)
        {
            if (data == null || data.schemaVersion > 2) throw new InvalidOperationException("Unsupported career save version.");
            data.schemaVersion = 2; data.highestUnlocked = Math.Max(0, data.highestUnlocked);
            data.completedRuns = Math.Max(0, data.completedRuns);
            data.wallet = Math.Max(0, Math.Min(MaxMoney, data.wallet));
            data.totalArrivalScore = Math.Max(0, data.totalArrivalScore); data.bestArrivalScore = Math.Max(0, data.bestArrivalScore);
            if (data.vehicles == null) data.vehicles = new List<OwnedVehicle>();
            var ids = new HashSet<string>();
            data.vehicles.RemoveAll(v => v == null || Find(v.id) == null || !ids.Add(v.id));
            if (!ids.Contains("sedan")) data.vehicles.Insert(0, new OwnedVehicle { id = "sedan" });
            foreach (var car in data.vehicles)
            {
                car.engine = Math.Max(0, Math.Min(MaxUpgrade, car.engine)); car.tires = Math.Max(0, Math.Min(MaxUpgrade, car.tires));
                car.armor = Math.Max(0, Math.Min(MaxUpgrade, car.armor)); car.safe = Math.Max(0, Math.Min(MaxUpgrade, car.safe));
            }
            if (!data.vehicles.Exists(v => v.id == data.selectedVehicle)) data.selectedVehicle = "sedan";
            if (data.settledRuns == null) data.settledRuns = new List<string>();
        }
        public static int CashLoss(int currentLoot, float actualDamage, float dollarsPerDamage, int safeLevel)
        {
            if (currentLoot <= 0 || actualDamage <= 0 || float.IsNaN(actualDamage) || float.IsInfinity(actualDamage)) return 0;
            double amount = Math.Ceiling(actualDamage * Math.Max(0, dollarsPerDamage) * (1 - 0.2 * Math.Max(0, Math.Min(3, safeLevel))));
            return (int)Math.Min(currentLoot, amount);
        }
    }
    public sealed class CareerAccount
    {
        public CareerData Data { get; private set; }
        readonly Func<CareerData, bool> persist;
        public CareerAccount(CareerData data, Func<CareerData, bool> save)
        { GarageCatalog.Normalize(data); Data = data; persist = save; }
        public OwnedVehicle Selected => Data.vehicles.Find(v => v.id == Data.selectedVehicle);
        bool Commit(CareerData next, out string message)
        {
            bool saved;
            try { saved = persist(next); } catch { saved = false; }
            if (!saved) { message = "Save failed. Nothing was charged. Check disk access and retry."; return false; }
            Data = next; message = "Saved."; return true;
        }
        public bool BuyVehicle(string id, out string message)
        {
            var spec = GarageCatalog.Find(id);
            if (spec == null) { message = "Unknown vehicle."; return false; }
            if (Data.vehicles.Exists(v => v.id == id)) { message = "Already owned."; return false; }
            if (Data.wallet < spec.Price) { message = "Not enough money."; return false; }
            var next = Data.Copy(); next.wallet -= spec.Price; next.vehicles.Add(new OwnedVehicle { id = id });
            return Commit(next, out message);
        }
        public bool SelectVehicle(string id, out string message)
        {
            if (!Data.vehicles.Exists(v => v.id == id)) { message = "Buy this vehicle first."; return false; }
            var next = Data.Copy(); next.selectedVehicle = id; return Commit(next, out message);
        }
        public bool BuyUpgrade(UpgradeKind kind, out string message)
        {
            if (!Enum.IsDefined(typeof(UpgradeKind), kind)) { message = "Unknown upgrade."; return false; }
            if (GarageCatalog.Level(Selected, kind) >= GarageCatalog.MaxUpgrade) { message = "Maximum level."; return false; }
            int cost = GarageCatalog.UpgradePrice(Selected, kind);
            if (Data.wallet < cost) { message = "Not enough money."; return false; }
            var next = Data.Copy(); next.wallet -= cost;
            var car = next.vehicles.Find(v => v.id == next.selectedVehicle);
            switch (kind) { case UpgradeKind.Engine: car.engine++; break; case UpgradeKind.Tires: car.tires++; break; case UpgradeKind.Armor: car.armor++; break; case UpgradeKind.Safe: car.safe++; break; }
            return Commit(next, out message);
        }
        public bool Settle(string runId, int loot, int score, int nextStage, out string message)
        {
            if (string.IsNullOrEmpty(runId)) { message = "Invalid run."; return false; }
            if (Data.settledRuns.Contains(runId)) { message = "Already paid."; return true; }
            var next = Data.Copy();
            next.wallet = Math.Min(GarageCatalog.MaxMoney, next.wallet + Math.Max(0, loot));
            next.highestUnlocked = Math.Max(next.highestUnlocked, nextStage);
            next.completedRuns = Math.Min(int.MaxValue - 1, next.completedRuns) + 1;
            next.totalArrivalScore = Math.Min(long.MaxValue - Math.Max(0, score), next.totalArrivalScore) + Math.Max(0, score);
            next.bestArrivalScore = Math.Max(next.bestArrivalScore, score); next.settledRuns.Add(runId);
            return Commit(next, out message);
        }
    }
}
