using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

using OneUnified.GeneticProgramming;

using SmartQuant;
//using SmartQuant.FIX;
//using SmartQuant.Data;
//using SmartQuant.Series;
using SmartQuant.Trading;
//using SmartQuant.Charting;
//using SmartQuant.Execution;
//using SmartQuant.Providers;
//using SmartQuant.Indicators;
using SmartQuant.Instruments;
using SmartQuant.Optimization;
//using SmartQuant.Testing;

namespace UI {

  public class optimizer : OptimizationManager {
    public override void Init() {
      //Console.WriteLine("In OptimizationManager Init");
      // Add your code here
    }

    public override double Objective() {
      double ob;
      //ob = MetaStrategyBase.Tester.Components["Drawdown"].LastValue;
      //ob = MetaStrategyBase.Tester.Components["AverageGain"].LastValue;
      //ob = MetaStrategyBase.Tester.Components["AverageLoss"].LastValue;
      //ob = MetaStrategyBase.Tester.Components["Number Of RoundTrips"].LastValue;
      //ob = MetaStrategyBase.Tester.Components["PnL"].LastValue;
      //ob = MetaStrategyBase.Tester.Components["Profit Factor"].LastValue;
      //ob = MetaStrategyBase.Tester.Components["Profit Per Winning Trade"].LastValue;
      ob = MetaStrategyBase.Tester.Components["FinalWealth"].LastValue;
      double w = MetaStrategyBase.Portfolio.Transactions.Count;
      //double a1 = MetaStrategyBase.Portfolio.Performance.Drawdown;
      //double a2 = MetaStrategyBase.Portfolio.Performance.PnL;
      if (0 == w) {
        ob = 0;
      }
      else {
        
      }
      //double a3 = MetaStrategyBase.Portfolio.Account.
      //Console.WriteLine( "optimizer {0}", v );
      //return base.Objective();
      Console.Write("FW={0:#.00} DrawDown={1:#.00} #Trans={2} #RT={3} AG={4:#.00} AL={5:#.00} PF={6:#.00} PPWT={7:#.00}",
        MetaStrategyBase.Tester.Components["FinalWealth"].LastValue,
        MetaStrategyBase.Tester.Components["Drawdown"].LastValue,
        MetaStrategyBase.Portfolio.Transactions.Count,
        MetaStrategyBase.Tester.Components["Number Of RoundTrips"].LastValue,
        0, 0,
      //MetaStrategyBase.Tester.Components["AverageGain"].LastValue,
      //MetaStrategyBase.Tester.Components["AverageLoss"].LastValue,
      MetaStrategyBase.Tester.Components["Profit Factor"].LastValue,
      MetaStrategyBase.Tester.Components["Profit Per Winning Trade"].LastValue
      );
      Console.WriteLine(" Cost={0}", MetaStrategyBase.Tester.Components["Cost"].LastValue);
      return ob;
    }

  }

  class Startup {
    ATSMetaStrategy meta;
    ATSStrategy strat;

    public Startup() {
      Framework.Init();
      DataManager.Init();
       Framework.LoadPlugins();

    }

    public void Optimize() {
      if (true) {
        IComponentBase ic;

        meta = new ATSMetaStrategy("metagp");
        strat = new ATSStrategy("ATS", "Test ATS Strategy");

        //StrategyComponentManager.RegisterDefaultComponent(typeof(atscQT));
        //ic = StrategyComponentManager.GetComponent("{1bffce05-3830-484b-aabc-21297ab3fc2a}", this);
        //strat.SetComponent(ComponentType.ATSComponent, ic);

        StrategyComponentManager.RegisterDefaultComponent(typeof(atscPivot));
        ic = StrategyComponentManager.GetComponent("{55e7fcd2-3e34-4218-8d01-571c6c54fb4d}", this);
        strat.SetComponent(ComponentType.ATSComponent, ic);

        StrategyComponentManager.RegisterDefaultComponent(typeof(mktSingle));
        ic = StrategyComponentManager.GetComponent("{0c19f913-f3e5-4c53-9908-4b87403f0616}", this);
        strat.SetComponent(ComponentType.MarketManager, ic);

        //StrategyComponentManager.RegisterDefaultComponent(typeof(mktToTrade));
        //ic = StrategyComponentManager.GetComponent("{f6e7b160-1d92-4f46-af68-8d451783addc}", this);
        //strat.SetComponent(ComponentType.MarketManager, ic);

        meta.Add(strat);
        Console.WriteLine("meta.strategies.count {0}", meta.Strategies.Count);

        StrategyComponentManager.RegisterDefaultComponent(typeof(simQuotesAndTicks));
        ic = StrategyComponentManager.GetComponent("{52728743-527f-4e40-a128-dd37387e6304}", this); 
        meta.SetComponent(ComponentType.SimulationManager, ic);

        meta.MetaStrategyMode = MetaStrategyMode.Simulation;
        meta.SimulationManager.Init();

        meta.OptimizationManager = new optimizer();
        //meta.Optimizer = new BruteForceTest(meta);
        meta.Optimizer = new GPOptimizer(meta);
        meta.OptimizerType = EOptimizerType.GeneticAlgorithm;

        meta.Optimizer.UpdateCalled += new EventHandler(Optimizer_UpdateCalled);
        meta.Optimizer.StepCalled += new EventHandler(Optimizer_StepCalled);
        meta.Optimizer.ObjectiveCalled += new EventHandler(Optimizer_ObjectiveCalled);
        meta.Optimizer.Inited += new EventHandler(Optimizer_Inited);
        meta.Optimizer.BestObjectiveReceived += new EventHandler(Optimizer_BestObjectiveRecieved);
        meta.Optimizer.CircleCalled += new EventHandler(Optimizer_CircleCalled);

        meta.GetOptimizationParameters();

        meta.Started += new EventHandler(meta_Started);
        meta.Stopped += new EventHandler(meta_Stopped);

        //meta.DesignMode = false;
        meta.Optimize();
      }

    }

    public void PrintGeneration() {
    }

    public void Stop() {
      meta.StopOptimization();
    }

    void meta_Stopped( object sender, EventArgs e ) {
      //throw new Exception("The method or operation is not implemented.");
      //Console.WriteLine(" meta stopped");
    }

    void meta_Started( object sender, EventArgs e ) {
      //throw new Exception("The method or operation is not implemented.");
      //Console.WriteLine(" meta started");
    }

    void Optimizer_CircleCalled( object sender, EventArgs e ) {
      //throw new Exception("The Optimizer_CircleCalled or operation is not implemented.");
      //Console.WriteLine("Optimizer_CircleCalled called.");
    }

    void Optimizer_BestObjectiveRecieved( object sender, EventArgs e ) {
      //throw new Exception("The Optimizer_BestObjectiveRecieved or operation is not implemented.");
      //double d = meta.Optimizer.
      //Console.WriteLine("Optimizer_BestObjectiveRecieved called. {0}", meta.Optimizer.LastObjective);
    }

    void Optimizer_Inited( object sender, EventArgs e ) {
      //throw new Exception("The Optimizer_Inited or operation is not implemented.");
      //Console.WriteLine("Optimizer_Inited called.");
      Console.WriteLine(" Init'd {0}", meta.Optimizer.GetNParam());
    }

    void Optimizer_ObjectiveCalled( object sender, EventArgs e ) {
      //throw new Exception("The Optimizer_ObjectiveCalled or operation is not implemented.");
      //Console.WriteLine("Optimizer_ObjectiveCalled called.");
    }

    void Optimizer_StepCalled( object sender, EventArgs e ) {
      //throw new Exception("The Optimizer_StepCalled or operation is not implemented.");
      //Console.WriteLine("Optimizer_StepCalled called.");
    }

    void Optimizer_UpdateCalled( object sender, EventArgs e ) {
      //throw new Exception("The Optimizer_UpdateCalled or operation is not implemented.");
      //Console.WriteLine("Optimizer_UpdateCalled called.");
    }

  }
}


