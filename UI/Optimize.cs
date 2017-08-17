using System;
using System.Collections.Generic;
using System.Text;

using SmartQuant;
using SmartQuant.FIX;
using SmartQuant.Data;
using SmartQuant.Series;
using SmartQuant.Trading;
using SmartQuant.Charting;
using SmartQuant.Execution;
using SmartQuant.Providers;
using SmartQuant.Indicators;
using SmartQuant.Instruments;
using SmartQuant.Optimization;

namespace UI {
  /// <summary>
  /// Brute force optimizer
  /// 
  /// The brute force optimizer is a simple iteration
  /// over the parameter space; Steps parameter defines the step width
  /// from one iteration to the other.
  /// 
  /// Depandant on the example it can be faster than other techniques and give a 
  /// true optimum. It does not make any preassumptions on the search space.
  /// 
  /// </summary>
  /// <example>
  /// <code>
  ///using System;
  ///using RQuant.ParamSet;
  ///using RQuant.Optimization;
  /// 
  ///namespace OptimizationTest
  ///{
  /// 
  ///	class Function : IOptimizable 
  ///	{
  ///		double x;
  ///		double y;
  /// 
  ///		public Function()
  ///		{
  ///			x = 100;
  ///			y = 100;
  ///		}
  /// 
  ///		public void Init(TParamSet ParamSet)
  ///		{
  ///			ParamSet.SetNParam(2);
  /// 
  ///			ParamSet.SetParam(0, x);
  ///			ParamSet.SetParam(1, y);
  /// 
  ///			ParamSet.SetLowerBound(0, -100);
  ///			ParamSet.SetLowerBound(1, -100);
  /// 
  ///			ParamSet.SetUpperBound(0,  100);
  ///			ParamSet.SetUpperBound(0,  100);
  ///			
  ///			ParamSet.SetSteps(0,  0.01);
  ///			ParamSet.SetSteps(1,  0.01);
  ///		}
  /// 
  ///		public void Update(TParamSet ParamSet)
  ///		{
  ///			x = ParamSet.GetParam(0);
  ///			y = ParamSet.GetParam(1);
  ///		}
  /// 
  ///		public double Objective()
  ///		{
  ///			return (x - 10)*(x - 10) + (y + 5) * (y + 5);
  ///		}
  /// 
  ///		public void OnStep  () { ; }
  ///		public void OnCircle() { ; }
  /// 
  ///		public void Print()
  ///		{
  ///			Console.WriteLine("x = {0}, y = {1}", x, y);
  ///		}
  ///	}
  /// 
  ///	class MainClass
  ///	{
  /// 
  ///		static void Main(string[] args)
  ///		{
  ///			Function F = new Function();
  /// 
  ///			TOptimizer Optimizer = new TCoordinateDescent(F);
  /// 
  ///			F.Print();
  /// 
  ///			Optimizer.Optimize();
  /// 
  ///			F.Print();
  /// 
  ///			F = new Function();
  /// 
  ///			Optimizer = new TSimulatedAnnealing(F);
  /// 
  ///			F.Print();
  /// 
  ///			Optimizer.Optimize();
  /// 
  ///			F.Print();
  ///				F = new Function();
  /// 
  ///			Optimizer = new TBruteForce(F);
  /// 
  ///			F.Print();
  /// 
  ///			Optimizer.Optimize();
  /// 
  ///			F.Print();
  ///		}
  ///	}
  /// 	
  ///}
  /// </code>
  /// </example>
  public class BruteForceTest : Optimizer {
    double fOldObjective;
    double fNewObjective;
    double[] fOptParam;

    public BruteForceTest( IOptimizable Optimizable )
      : base(Optimizable) {
      // Coordinate descent normal constructor

      fType = EOptimizerType.BruteForce;

      Console.WriteLine("BruteForceTest Construct");

    }

    // recursively caling Optimize
    void OptimizeParam( int i ) {
      Console.WriteLine("BruteForceTest Optimize Param i {0}", i);
      if (stopped)
        return;
      // Return true if better parameter value is found
      if (i < fNParam) {
        if (fIsParamFixed[i]) // skip fixed parameters
          OptimizeParam(i++);
        else {
          for (double pv = fLowerBound[i]; pv <= fUpperBound[i]; pv += fSteps[i]) {
            fParam[i] = pv;
            OptimizeParam((i + 1));
          }
        }
      }
      else {
        Step();
      }
    }

    /// <summary>
    ///	Perform one optimizer step
    /// <br></br>
    /// <br></br>
    ///	One iteration step is done
    /// </summary>
    public void Step() {
      Console.WriteLine("BruteForceTest Step");
      Update();
      fNewObjective = Objective();
      if (fNewObjective < fOldObjective) {
        fOldObjective = fNewObjective;
        for (int j = 0; j < fNParam; j++) fOptParam[j] = fParam[j];
      }
      OnStep();
//      Application.DoEvents();
    }

    public override double Objective() {
      Console.WriteLine("BruteForceTest Objective");
      return base.Objective();
    }

    public override void OnCircle() {
      Console.WriteLine("BruteForceTest OnCircle");
      base.OnCircle();
    }

    public override void OnStep() {
      Console.WriteLine("BruteForceTest OnStep");
      base.OnStep();
    }

    public override void Update() {
      Console.WriteLine("BruteForceTest Update");
      base.Update();
    }

    public override void Optimize() {
      int i;

      // Brute Force
      base.Optimize();
      fOptParam = new double[fNParam];
      fOldObjective = double.MaxValue - 1;
      OptimizeParam(0);
      for (i = 0; i < fNParam; i++) fParam[i] = fOptParam[i];
      Update();

      EmitBestObjectiveReceived();

      fNewObjective = Objective();

      // ???
      base.Update();

      if (fVerboseMode == EVerboseMode.Debug) {
        for (i = 0; i < fNParam; i++)
          Console.WriteLine("Param[{0}] = {1}", i, fParam[i]);

        base.Print();
      }

      EmitCompleted();
    }

    /// <summary>
    /// Print
    /// </summary>
    public override void Print() {
      base.Print();
    }
  }
}


/************************************************************************
 * Copyright(c) 1997-2004, SmartQuant Ltd. All rights reserved.         *
 *                                                                      *
 * This file is provided as is with no warranty of any kind, including  *
 * the warranty of design, merchantibility and fitness for a particular *
 * purpose.                                                             *
 *                                                                      *
 * This software may not be used nor distributed without proper license *
 * agreement.                                                           *
 ************************************************************************/

