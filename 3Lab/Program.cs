using System.Collections.Generic;
using System.Linq;
using System;
public abstract class LivingOrganism
{
    public double Energy { get; protected set; }
    public int Age { get; protected set; }
    public double Size { get; protected set; }
    public bool IsAlive { get; protected set; }

    protected LivingOrganism(double energy, int age, double size)
    {
        Energy = energy;
        Age = age;
        Size = size;
        IsAlive = true;
    }
    public abstract void Update();
    public virtual void Die()
    {
        if (!IsAlive) return;
        IsAlive = false;
        Console.WriteLine($"{this.GetType().Name} (Size: {Size:F1}) has died at age {Age}.");
    }
    public virtual double BeEaten(double energyDrained)
    {
        double energyTaken = Math.Min(this.Energy, energyDrained);
        this.Energy -= energyTaken;

        if (this.Energy <= 0)
        {
            this.Energy = 0;
            Die();
        }
        return energyTaken;
    }
}
public interface IReproducible
{
    LivingOrganism Reproduce();
}
public interface IPredator
{

    void Hunt(LivingOrganism prey);
}
public class Plant : LivingOrganism, IReproducible
{
    public double PhotosynthesisRate { get; private set; }
    private const double ReproductionEnergyCost = 20;

    public Plant(double energy, int age, double size, double photosynthesisRate)
        : base(energy, age, size)
    {
        PhotosynthesisRate = photosynthesisRate;
    }
    public override void Update()
    {
        if (!IsAlive) return;

        Age++;
        Energy += PhotosynthesisRate;
        Energy -= Size * 0.1;

        if (Energy <= 0)
        {
            Die();
        }
        else if (Age > 50)
        {
            Die();
        }
        else if (Size < 15)
        {
            Size += 0.1;
            Energy -= 0.5;
        }
    }
    public LivingOrganism Reproduce()
    {
        if (Energy > ReproductionEnergyCost && Age > 5)
        {
            Energy -= ReproductionEnergyCost;
            Console.WriteLine("Plant has reproduced.");
            return new Plant(10, 0, 1, this.PhotosynthesisRate);
        }
        return null;
    }
}
public class Animal : LivingOrganism, IReproducible, IPredator
{
    public double MetabolismRate { get; private set; }
    public double Speed { get; private set; }
    private const double ReproductionEnergyCost = 50;
    public Animal(double energy, int age, double size, double metabolismRate, double speed)
        : base(energy, age, size)
    {
        MetabolismRate = metabolismRate;
        Speed = speed;
    }

    public override void Update()
    {
        if (!IsAlive) return;

        Age++;
        Energy -= (Size * MetabolismRate);

        if (Energy <= 0)
        {
            Die();
        }
        else if (Age > 70)
        {
            Die();
        }
    }
    public void Hunt(LivingOrganism prey)
    {
        if (!prey.IsAlive || !this.IsAlive) return;
        if (prey is Animal otherAnimal)
        {
            if (this.Speed > otherAnimal.Speed)
            {
                Console.WriteLine($"Animal (Speed: {this.Speed}) hunted another Animal (Speed: {otherAnimal.Speed})!");
                double energyGained = otherAnimal.BeEaten(otherAnimal.Energy);
                this.Energy += energyGained * 0.8;
            }
            else
            {
                Energy -= 5;
            }
        }
    }
    public void Graze(Plant plant)
    {
        if (!plant.IsAlive || !this.IsAlive) return;

        Console.WriteLine("Animal is grazing on a Plant.");
        double energyGained = plant.BeEaten(plant.Size * 5);
        this.Energy += energyGained * 0.5;
    }
    public LivingOrganism Reproduce()
    {
        if (Energy > ReproductionEnergyCost && Age > 10)
        {
            Energy -= ReproductionEnergyCost;
            Console.WriteLine("Animal has reproduced.");
            return new Animal(30, 0, 2, this.MetabolismRate, this.Speed);
        }
        return null;
    }
}
public class Microorganism : LivingOrganism, IReproducible
{
    public double DecompositionRate { get; private set; }
    private const double ReproductionEnergyCost = 5;
    public Microorganism(double energy, int age, double size, double decompositionRate)
        : base(energy, age, size)
    {
        DecompositionRate = decompositionRate;
    }
    public override void Update()
    {
        if (!IsAlive) return;
        Age++;
        Energy -= 0.2;
        if (Energy <= 0) Die();
        if (Age > 100) Die();
    }
    public void Decompose(LivingOrganism deadOrganism)
    {
        if (deadOrganism.IsAlive || deadOrganism.Energy <= 0) return;
        double energyToDecompose = deadOrganism.Energy * DecompositionRate;
        double energyGained = deadOrganism.BeEaten(energyToDecompose);
        this.Energy += energyGained;
    }
    public LivingOrganism Reproduce()
    {
        if (Energy > ReproductionEnergyCost)
        {
            Energy -= ReproductionEnergyCost;
            return new Microorganism(2, 0, 0.1, this.DecompositionRate);
        }
        return null;
    }
}

public class Ecosystem
{
    private List<LivingOrganism> organisms;
    private Random random = new Random();

    public Ecosystem()
    {
        organisms = new List<LivingOrganism>();
    }

    public void AddOrganism(LivingOrganism organism)
    {
        organisms.Add(organism);
    }

    public void SimulateTick()
    {
        List<LivingOrganism> newOrganisms = new List<LivingOrganism>();
        foreach (var organism in organisms.ToList())
        {
            if (!organism.IsAlive) continue;
            organism.Update();
            if (!organism.IsAlive) continue;
            PerformActions(organism);
            if (organism is IReproducible reproducible)
            {
                var offspring = reproducible.Reproduce();
                if (offspring != null)
                {
                    newOrganisms.Add(offspring);
                }
            }
        }
        organisms.RemoveAll(o => !o.IsAlive && o.Energy <= 0);
        organisms.AddRange(newOrganisms);

        Console.WriteLine($"--- Tick End: {organisms.Count(o => o.IsAlive)} organisms alive. ---");
    }
    private void PerformActions(LivingOrganism organism)
    {
        if (organism is Animal animal)
        {
            var plant = FindRandomTarget<Plant>(animal);
            if (plant != null)
            {
                animal.Graze(plant);
            }

            var prey = FindRandomTarget<Animal>(animal, o => o != animal);
            if (prey != null)
            {
                animal.Hunt(prey);
            }
        }
        else if (organism is Microorganism micro)
        {
            var deadBody = organisms.FirstOrDefault(o => !o.IsAlive && o.Energy > 0);
            if (deadBody != null)
            {
                micro.Decompose(deadBody);
            }
        }
    }
    private T FindRandomTarget<T>(LivingOrganism self, Func<T, bool> predicate = null) where T : LivingOrganism
    {
        var query = organisms.OfType<T>().Where(o => o.IsAlive && o != self);

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var potentialTargets = query.ToList();
        if (potentialTargets.Count > 0)
        {
            return potentialTargets[random.Next(potentialTargets.Count)];
        }
        return null;
    }
    public void PrintStatus()
    {
        var groups = organisms.Where(o => o.IsAlive)
                                .GroupBy(o => o.GetType().Name)
                                .Select(g => new { Type = g.Key, Count = g.Count(), AvgEnergy = g.Average(o => o.Energy) });

        Console.WriteLine("\n=== Ecosystem Status ===");
        foreach (var group in groups)
        {
            Console.WriteLine($"* {group.Type}s: {group.Count} (Avg Energy: {group.AvgEnergy:F1})");
        }
        int deadCount = organisms.Count(o => !o.IsAlive && o.Energy > 0);
        Console.WriteLine($"* Dead Biomass (waiting for decomposition): {deadCount}");
        Console.WriteLine("=========================");
    }
}
public class Program
{
    public static void Main(string[] args)
    {
        Ecosystem world = new Ecosystem();
        for (int i = 0; i < 15; i++)
        {
            world.AddOrganism(new Plant(energy: 30, age: 0, size: 5, photosynthesisRate: 4));
        }
        for (int i = 0; i < 5; i++)
        {
            world.AddOrganism(new Animal(energy: 100, age: 0, size: 10, metabolismRate: 1.2, speed: 10));
        }
        world.AddOrganism(new Animal(energy: 150, age: 5, size: 15, metabolismRate: 1.5, speed: 15));
        for (int i = 0; i < 20; i++)
        {
            world.AddOrganism(new Microorganism(energy: 10, age: 0, size: 0.1, decompositionRate: 0.3));
        }
        world.PrintStatus();
        Console.WriteLine("\nStarting simulation... Press Enter to stop.");
        int tick = 0;
        while (true)
        {
            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter)
            {
                Console.WriteLine("Simulation stopped by user.");
                break;
            }
            tick++;
            Console.WriteLine($"\n--- Simulating Tick {tick} ---");
            world.SimulateTick();
            world.PrintStatus();
            System.Threading.Thread.Sleep(1000);
        }
    }
}

