using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

using System.Data;
using System.Reflection;
using System.Data.SqlClient;

using QDCustom;
using OneUnified;
using OneUnified.IQFeed;
using OneUnified.IQFeed.Forms;

//using OneUnified.IB;
using OneUnified.GeneticProgramming;

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

// http://www.savvis.net/corp
// http://www.radianz.com/public/home.asp
// http://www.interactivebrokers.com/en/software/ctci/gatewaySoftware.php?ib_entity=llc

namespace UI {

  /*
public class GPStrategy {
  public bool ExitSignal;
  public bool BuySignal;
  public bool SellSignal;
  public TimeSpan TradingTimeBegin;
  public TimeSpan TradingTimeEnd;

  public GPStrategy() {
  }

  public void Init() {
  }

  public void Reset() {
    ExitSignal = false;
    BuySignal = false;
    SellSignal = false;
  }
}
   */

  #region TransactionSet

  public class RoundTrip {

    SingleOrder EntryOrder;
    SingleOrder ExitOrder;

    DateTime dtEntryInitiation;
    DateTime dtEntryCompletion;
    DateTime dtExitInitiation;
    DateTime dtExitCompletion;

    Quote quoteEntryInitiation;
    Quote quoteEntryCompletion;
    Quote quoteExitInitiation;
    Quote quoteExitCompletion;

    Instrument instrument;

    PositionSide side;

    Quote latestQuote;

    public double MaxProfit = 0;
    public double CurrentProfit = 0;
    public double MaxProfitBeforeDown = 0;

    int TotalLong = 0;
    int TotalShrt = 0;

    bool ReportActive = true;

    public static int id = 0;
    int Id = 0;

    enum State {
      Created, EntrySubmitted, EntryFilled, HardStopSubmitted, SoftStopSubmitted, ExitSubmitted, ExitFilled, Done
    }
    State state = State.Created;

    public RoundTrip( Instrument instrument ) {
      this.instrument = instrument;
      Id = ++id;
    }

    public void Enter( Quote quote, SingleOrder order ) {
      latestQuote = quote;
      order.ExecutionReport += new ExecutionReportEventHandler(order_ExecutionReport);
      EntryOrder = order;
      if (State.Created == state) {
      }
      else {
        Console.WriteLine("*** {0} RoundTrip.Enter in {1}", instrument.Symbol, state);
      }
      //Console.WriteLine( "setting entry submitted" );
      state = State.EntrySubmitted;
      dtEntryInitiation = quote.DateTime;
      quoteEntryInitiation = quote;
      if (order.Side == SmartQuant.FIX.Side.Buy) side = PositionSide.Long;
      if (order.Side == SmartQuant.FIX.Side.Sell) side = PositionSide.Short;
      // need an error condition here
    }

    public void HardStop( Quote quote, SingleOrder order ) {
      latestQuote = quote;
      order.ExecutionReport += new ExecutionReportEventHandler(order_ExecutionReport);
      ExitOrder = order;
      if (State.EntryFilled == state) {
      }
      else {
        Console.WriteLine("*** {0} RoundTrip.HardStop in {1}", instrument.Symbol, state);
      }
      state = State.HardStopSubmitted;
      dtExitInitiation = quote.DateTime;
      quoteExitInitiation = quote;
    }

    public void SoftStop( Quote quote, SingleOrder order ) {
      latestQuote = quote;
      order.ExecutionReport += new ExecutionReportEventHandler(order_ExecutionReport);
      ExitOrder = order;
      if (State.HardStopSubmitted == state) {
      }
      else {
        Console.WriteLine("*** {0} RoundTrip.SoftStop in {1}", instrument.Symbol, state);
      }
      state = State.SoftStopSubmitted;
      dtExitInitiation = quote.DateTime;
      quoteExitInitiation = quote;
    }

    public void Exit( Quote quote, SingleOrder order ) {
      latestQuote = quote;
      order.ExecutionReport += new ExecutionReportEventHandler(order_ExecutionReport);
      ExitOrder = order;
      //Console.WriteLine( "setting exit submitted" );
      if (State.EntryFilled == state || State.HardStopSubmitted == state) {
      }
      else {
        Console.WriteLine("*** {0} RoundTrip.Exit in {1}", instrument.Symbol, state);
      }
      state = State.ExitSubmitted;
      dtExitInitiation = quote.DateTime;
      quoteExitInitiation = quote;
    }

    public void UpdateQuote( Quote quote ) {
      //Console.WriteLine(quote);
      latestQuote = quote;
      if (State.EntryFilled == state || State.HardStopSubmitted == state) {
        switch (side) {
          case PositionSide.Long:
            CurrentProfit = quote.Bid - EntryOrder.AvgPx;
            MaxProfit = Math.Max(MaxProfit, CurrentProfit);
            break;
          case PositionSide.Short:
            CurrentProfit = EntryOrder.AvgPx - quote.Ask;
            MaxProfit = Math.Max(MaxProfit, CurrentProfit);
            break;
        }
      }
    }

    public void Check() {
    }

    public void Report() {

      if (ReportActive) {
        //Console.WriteLine( "{0} {1} {2} {3}", 
        //	quoteEntryInitiation.DateTime.ToString("HH:mm:ss.fff"), 
        //	quoteEntryCompletion.DateTime.ToString("HH:mm:ss.fff"), 
        //	quoteExitInitiation.DateTime.ToString("HH:mm:ss.fff")  );
        //	quoteExitCompletion.DateTime.ToString("HH:mm:ss.fff"), 
        TimeSpan tsEntryDelay = quoteEntryCompletion.DateTime - quoteEntryInitiation.DateTime;
        string sEntryDelay = tsEntryDelay.Seconds.ToString("D2") + "." + tsEntryDelay.Milliseconds.ToString("D3");
        //string sEntryDelay = tsEntryDelay.ToString("HH:mm:ss.fff"); 

        TimeSpan tsExitDelay = quoteExitCompletion.DateTime - quoteExitInitiation.DateTime;
        string sExitDelay = tsExitDelay.Seconds.ToString("D2") + "." + tsExitDelay.Milliseconds.ToString("D3");

        TimeSpan tsTripDuration = quoteExitCompletion.DateTime - quoteEntryCompletion.DateTime;
        string sTripDuration = tsTripDuration.TotalSeconds.ToString("F3");

        double pl = 0.0;
        switch (side) {
          case PositionSide.Long:
            pl = ExitOrder.AvgPx - EntryOrder.AvgPx;
            break;
          case PositionSide.Short:
            pl = EntryOrder.AvgPx - ExitOrder.AvgPx;
            break;
        }
        // need two more values:  
        //   slippage on signal quote to entry price
        //   slippage on signal quote to exit price


        Console.Write("rt,{0,2},{1},{2},",
          Id,
          quoteEntryInitiation.DateTime.ToString("HH:mm:ss.fff"),
          quoteEntryCompletion.DateTime.ToString("HH:mm:ss.fff")
          );
        Console.Write("{0} {1}:{2,-5},{3},{4,3:#0.0},{5},{6,6:#0.00},{7,6:#0.00},",
          quoteExitCompletion.DateTime.ToString("HH:mm:ss.fff"),
          instrument.Symbol,
          side,
          sEntryDelay, sTripDuration, sExitDelay, MaxProfit, pl
          );

        Console.WriteLine(ExitOrder.Text);

        ReportActive = false;
      }
      else {
        Console.WriteLine("*** {0} Report called more than once.  Longs {1} Shorts {2}", TotalLong, TotalShrt);
      }

    }

    /*
    public static void FinalReport() {
      Console.WriteLine( "# Round Trips = {0}", alRoundTrips.Count );
    }
    */

    void order_ExecutionReport( object sender, ExecutionReportEventArgs args ) {

      ExecutionReport er;
      er = args.ExecutionReport;
      SingleOrder order = (SingleOrder)sender;

      /*
      Console.Write( "er '{0}',{1:#.00},{2},{3},{4},{5}",
      er.ClOrdID, er.AvgPx, er.Commission, er.CumQty, er.LastQty, er.OrderID  );
      Console.Write( ",{0},{1:#.00},{2},{3},{4}",
      er.OrdStatus, er.Price, er.Side, er.Tag, er.Text ); 
      Console.WriteLine(".");
      Console.WriteLine( "er State {0}", state );
      */

      // a validation that our buy side ultimately matches our sell side
      switch (order.Side) {
        case Side.Buy:
          TotalLong += (int)Math.Round(er.LastQty);
          break;
        case Side.Sell:
          TotalShrt += (int)Math.Round(er.LastQty);
          break;
      }

      switch (state) {
        case State.Created:
          throw new Exception("State.Create");
          break;
        case State.EntrySubmitted:
          //throw new Exception( "State.EntrySubmitted" );
          switch (er.OrdStatus) {
            case OrdStatus.Filled:
              state = State.EntryFilled;
              dtEntryCompletion = latestQuote.DateTime;
              quoteEntryCompletion = latestQuote;
              switch (side) {
                case PositionSide.Long:
                  MaxProfit = latestQuote.Bid - EntryOrder.AvgPx;
                  break;
                case PositionSide.Short:
                  MaxProfit = EntryOrder.AvgPx - latestQuote.Ask;
                  break;
              }
              break;
            case OrdStatus.Cancelled:
            case OrdStatus.New:
            case OrdStatus.PartiallyFilled:
            case OrdStatus.PendingCancel:
            case OrdStatus.PendingNew:
            case OrdStatus.Rejected:
            case OrdStatus.Stopped:
              break;
          }
          break;
        case State.EntryFilled:
          break;
        case State.HardStopSubmitted:
        case State.SoftStopSubmitted:
        case State.ExitSubmitted:
          //throw new Exception( "State.ExitSubmitted" );
          switch (er.OrdStatus) {
            case OrdStatus.Filled:
              state = State.ExitFilled;
              dtExitCompletion = latestQuote.DateTime;
              quoteExitCompletion = latestQuote;
              Report();
              break;
            case OrdStatus.Cancelled:
            case OrdStatus.New:
            case OrdStatus.PartiallyFilled:
            case OrdStatus.PendingCancel:
            case OrdStatus.PendingNew:
            case OrdStatus.Rejected:
            case OrdStatus.Stopped:
              break;
          }
          break;
        case State.ExitFilled:
          //throw new Exception( "State.ExitFilled" );
          break;
        case State.Done:
          //throw new Exception( "State.Done" );
          break;
      }
    }
  }

  //
  // need order round tip  as well as the existing transaction round trip statistics
  //

  public class TransactionSetEventHolder {

    public delegate void UpdateQuoteHandler( object source, Quote quote );
    public event UpdateQuoteHandler OnUpdateQuote;

    public delegate void StrategyStopHandler( object source, EventArgs e );
    public event StrategyStopHandler OnStrategyStop;

    public delegate void UpdateSignalStatusHandler( object source, bool Exited );
    public event UpdateSignalStatusHandler UpdateSignalStatus;

    public void UpdateQuote( object source, Quote quote ) {
      if (null != OnUpdateQuote) OnUpdateQuote(source, quote);
    }

    public void StrategyStop( object source, EventArgs e ) {
      if (null != OnStrategyStop) OnStrategyStop(source, e);
    }

    public void SignalStatus( object source, bool Exited ) {
      if (null != UpdateSignalStatus) UpdateSignalStatus(source, Exited);
    }

  }

  public class TransactionSet {

    TransactionSetEventHolder eventholder;

    bool UpdateQuoteActive = false;
    bool OnStrategyStopEventActive = false;

    public enum ESignal { Long, Short, ScaleIn, ScaleOut, Exit };
    ESignal EntrySignal;
    bool bDone = false;

    enum EState { Init, EntrySent, WaitForExit, CleanUp, Done };
    EState State = EState.Init;

    enum EOrderTag { Entry, Exit, HardStop, TrailingStop };
    bool MaintainStopOrder = false;

    Hashtable ordersSubmitted;
    Hashtable ordersPartiallyFilled;
    Hashtable ordersFilled;
    Hashtable ordersCancelled;
    Hashtable ordersRejected;

    Hashtable ordersTag;

    Quote quote;
    double JumpDelta;
    double HardStopDelta;
    double TrailingStopDelta;
    Instrument instrument;
    string Symbol;
    int quanInitial;
    int quanScaling;
    int quanMax;

    //string HardStopClOrdID = "";  // track our existing stop order
    double HardStopPrice;
    double SoftStopPrice;
    double AvgEntryPrice;  // price at which the entry filled

    bool OutStandingOrdersExist = false;

    int PositionRequested = 0;  // positive for long negative for short
    int PositionFilled = 0;		// positive for long negative for short

    RoundTrip trip;

    ATSComponent atsc;

    public TransactionSet(
      ESignal Signal, ATSComponent atsc,
      int InitialQuantity, int ScalingQuantity, int MaxQuantity,
      Quote quote, double JumpDelta,
      double HardStopDelta, double TrailingStopDelta,
      TransactionSetEventHolder eventholder
      ) {

      //Console.WriteLine( "{0} Entered Transaction Set", quote.DateTime.ToString("HH:mm:ss.fff") );

      this.eventholder = eventholder;

      eventholder.OnUpdateQuote += OnUpdateQuote;
      OnStrategyStopEventActive = true;
      eventholder.OnStrategyStop += OnStrategyStop;
      OnStrategyStopEventActive = true;

      this.EntrySignal = Signal;
      this.JumpDelta = JumpDelta;  // extra bit for setting a limit order
      this.HardStopDelta = HardStopDelta;
      this.TrailingStopDelta = TrailingStopDelta;
      this.quote = quote;
      this.atsc = atsc;
      instrument = atsc.Instrument;
      Symbol = instrument.Symbol;
      quanInitial = InitialQuantity;
      quanScaling = ScalingQuantity;
      quanMax = MaxQuantity;

      trip = new RoundTrip(instrument);

      ordersSubmitted = new Hashtable(10);
      ordersPartiallyFilled = new Hashtable(10);
      ordersFilled = new Hashtable(10);
      ordersCancelled = new Hashtable(10);
      ordersRejected = new Hashtable(10);
      ordersTag = new Hashtable(10);

      if (!((ESignal.Long == Signal) || (ESignal.Short == Signal))) {
        Console.WriteLine("Transaction Set Problem 1");
        throw new ArgumentException(instrument.Symbol + " has improper Entry Signal: " + Signal.ToString());
      }

      SingleOrder order;

      switch (Signal) {
        case ESignal.Long:
          order = atsc.MarketOrder(SmartQuant.FIX.Side.Buy, quanInitial);
          //entryOrder = new LimitOrder(instrument, SmartQuant.FIX.Side.Buy, Quantity, 4.55 );
          //order = atsc.StopOrder( SmartQuant.FIX.Side.Buy, quanInitial, Math.Round( quote.Ask + JumpDelta, 2 ) );
          order.Text = "Long Mkt Entr";
          ordersTag.Add(order.ClOrdID, EOrderTag.Entry);
          PositionRequested = quanInitial;
          State = EState.EntrySent;
          trip.Enter(quote, order);
          SendOrder(quote, order);
          break;
        case ESignal.Short:
          order = atsc.MarketOrder(SmartQuant.FIX.Side.Sell, quanInitial);
          //entryOrder = new LimitOrder(instrument, SmartQuant.FIX.Side.Buy, Quantity, 4.55 );
          //order = atsc.StopOrder( SmartQuant.FIX.Side.Sell, quanInitial, Math.Round( quote.Bid - JumpDelta, 2 ) );
          order.Text = "Shrt Mkt Entr";
          ordersTag.Add(order.ClOrdID, EOrderTag.Entry);
          PositionRequested = -quanInitial;
          State = EState.EntrySent;
          trip.Enter(quote, order);
          SendOrder(quote, order);
          break;
      }
    }

    private void SendOrder( Quote quote, SingleOrder order ) {
      order.ExecutionReport += new ExecutionReportEventHandler(order_ExecutionReport);
      order.StatusChanged += new EventHandler(order_StatusChanged);
      OutStandingOrdersExist = true;
      ordersSubmitted.Add(order.ClOrdID, order);
      order.Send();
      //Console.WriteLine( "{0} {1} sent", order.ClOrdID, order.Text );
    }

    private void SetHardStop() {
      SingleOrder order;
      switch (EntrySignal) {
        case ESignal.Long:
          order = atsc.StopOrder(SmartQuant.FIX.Side.Sell, Math.Abs(PositionFilled), HardStopPrice);
          order.Text = "Long Hard Stop";
          ordersTag.Add(order.ClOrdID, EOrderTag.HardStop);
          //PositionRequested = -Math.Abs( PositionFilled );
          State = EState.WaitForExit;
          trip.HardStop(quote, order);
          SendOrder(quote, order);
          break;
        case ESignal.Short:
          order = atsc.StopOrder(SmartQuant.FIX.Side.Buy, Math.Abs(PositionFilled), HardStopPrice);
          order.Text = "Shrt Hard Stop";
          ordersTag.Add(order.ClOrdID, EOrderTag.HardStop);
          //PositionRequested = Math.Abs( PositionFilled );
          State = EState.WaitForExit;
          trip.HardStop(quote, order);
          SendOrder(quote, order);
          break;
      }
    }

    private void CheckSoftStop() {
      // don't update if we have no position
      SingleOrder order;
      if (EState.WaitForExit == State & 0 != PositionFilled && trip.MaxProfit > 0) {
        double t;
        switch (EntrySignal) {
          case ESignal.Long:
            t = quote.Bid - TrailingStopDelta;
            if (t > SoftStopPrice) {
              SoftStopPrice = t;
            }
            if (quote.Bid < SoftStopPrice) {
              CancelSubmittedOrders();
              order = atsc.MarketOrder(SmartQuant.FIX.Side.Sell, Math.Abs(PositionFilled));
              order.Text = "Long Mkt Stop";
              ordersTag.Add(order.ClOrdID, EOrderTag.TrailingStop);
              PositionRequested -= Math.Abs(PositionFilled);
              State = EState.CleanUp;
              trip.SoftStop(quote, order);
              SendOrder(quote, order);
              //State = EState.CleanUp;
            }
            break;
          case ESignal.Short:
            t = quote.Ask + TrailingStopDelta;
            if (t < SoftStopPrice) {
              SoftStopPrice = t;
            }
            if (quote.Ask > SoftStopPrice) {
              CancelSubmittedOrders();
              order = atsc.MarketOrder(SmartQuant.FIX.Side.Buy, Math.Abs(PositionFilled));
              order.Text = "Shrt Mkt Stop";
              ordersTag.Add(order.ClOrdID, EOrderTag.TrailingStop);
              PositionRequested += Math.Abs(PositionFilled);
              State = EState.CleanUp;
              trip.SoftStop(quote, order);
              SendOrder(quote, order);
              //State = EState.CleanUp;
            }
            break;
        }
      }
    }

    void CancelSubmittedOrders() {
      if (0 < ordersSubmitted.Count) {
        //Console.WriteLine( "Cancelling {0} orders", ordersSubmitted.Count );
        Queue q = new Queue(10);
        foreach (SingleOrder order in ordersSubmitted.Values) {
          q.Enqueue(order);
        }
        while (0 != q.Count) {
          SingleOrder order = (SingleOrder)q.Dequeue();
          //Console.WriteLine( "{0} cancelling", order.ClOrdID );
          order.Cancel();
        }
      }
    }

    public void UpdateSignal( ESignal Signal ) {
      //Console.WriteLine( "In UpdateSignal" );
      if (this.EntrySignal == Signal) {
        // don't bother with stuff in the same direction, just keep monitoring the situation
      }
      else {
        switch (Signal) {
          case ESignal.Long:
          case ESignal.Short:
            throw new ArgumentException(
              instrument.Symbol + " has improper Update Signal: "
              + Signal.ToString() + " vs " + EntrySignal.ToString());
            break;
          case ESignal.ScaleIn:
            break;
          case ESignal.ScaleOut:
            break;
          case ESignal.Exit:
            //Console.WriteLine( "UpdateSignal {0} {1} {2} {3}", Signal, State, PositionRequested, PositionFilled );
            if (EState.WaitForExit == State || EState.EntrySent == State) {
              // cancel all outstanding orders
              CancelSubmittedOrders();
              // set flag so that if something gets filled, to unfill it right away 
              // later may want to keep it if things are going in the correct direction
              SingleOrder order;
              if (0 < PositionFilled) {
                order = atsc.MarketOrder(SmartQuant.FIX.Side.Sell, Math.Abs(PositionFilled));
                //entryOrder = new LimitOrder(instrument, SmartQuant.FIX.Side.Buy, Quantity, 4.55 );
                //entryOrder = new StopOrder(instrument, SmartQuant.FIX.Side.Sell, Quantity, quote.Bid - Jump );
                order.Text = "Long Mkt Exit";
                ordersTag.Add(order.ClOrdID, EOrderTag.Exit);
                PositionRequested -= Math.Abs(PositionFilled);
                State = EState.CleanUp;
                trip.Exit(quote, order);
                SendOrder(quote, order);
              }
              if (0 > PositionFilled) {
                order = atsc.MarketOrder(SmartQuant.FIX.Side.Buy, Math.Abs(PositionFilled));
                //entryOrder = new LimitOrder(instrument, SmartQuant.FIX.Side.Buy, Quantity, 4.55 );
                //entryOrder = new StopOrder(instrument, SmartQuant.FIX.Side.Buy, Quantity, quote.Ask + Jump );
                order.Text = "Shrt Mkt Exit";
                ordersTag.Add(order.ClOrdID, EOrderTag.Exit);
                PositionRequested += Math.Abs(PositionFilled);
                State = EState.CleanUp;
                trip.Exit(quote, order);
                SendOrder(quote, order);
              }
            }
            break;
        }
      }
    }

    void order_StatusChanged( object sender, EventArgs e ) {

      SingleOrder order = sender as SingleOrder;
      //Console.WriteLine( "*** {0} Status {1} cum {2} leaves {3} last {4} total {5}",
      //	instrument.Symbol, order.OrdStatus, order.CumQty, order.LeavesQty, order.LastQty, order.OrderQty );

      bool CheckStop = false;

      switch (order.OrdStatus) {
        case OrdStatus.PartiallyFilled:
          if (0 != order.LeavesQty) {
            Console.WriteLine("*** {0} Remaining quantity = {1}", instrument.Symbol, order.LeavesQty);
          }
          if (!ordersPartiallyFilled.ContainsKey(order.ClOrdID)) {
            if (ordersSubmitted.ContainsKey(order.ClOrdID)) {
              ordersSubmitted.Remove(order.ClOrdID);
            }
            ordersPartiallyFilled.Add(order.ClOrdID, order);
          }
          //CheckStop = true;  // need to fix this sometime
          break;
        case OrdStatus.Filled:
          //Console.WriteLine("Average fill price = {0}@{1:#.00} {2} {3}", order.OrderQty, order.AvgPx, order.Side, order.OrdStatus);
          if (ordersSubmitted.ContainsKey(order.ClOrdID)) {
            ordersSubmitted.Remove(order.ClOrdID);
          }
          if (ordersPartiallyFilled.ContainsKey(order.ClOrdID)) {
            ordersPartiallyFilled.Remove(order.ClOrdID);
          }
          ordersFilled.Add(order.ClOrdID, order);
          CheckStop = true;  // HardStopPrice on set on 'Filled'
          break;
        case OrdStatus.Cancelled:
          if (ordersSubmitted.ContainsKey(order.ClOrdID)) {
            ordersSubmitted.Remove(order.ClOrdID);
          }
          ordersCancelled.Add(order.ClOrdID, order);
          break;
        case OrdStatus.PendingCancel:
          // not used during simulation
          // signalled during realtime trading
          break;
        default:
          Console.WriteLine("*** {0} Order status changed to : {1}", instrument.Symbol, order.OrdStatus.ToString());
          break;
      }

      if (CheckStop) {
        if (ordersTag.ContainsKey(order.ClOrdID)) {
          EOrderTag tag = (EOrderTag)ordersTag[order.ClOrdID];
          if (EOrderTag.Entry == tag) {
            SetHardStop();
          }
          if (EOrderTag.HardStop == tag) {
            State = EState.CleanUp;
          }
        }
      }

      OutStandingOrdersExist = ((0 != ordersSubmitted.Count) || (0 != ordersPartiallyFilled.Count));
      //Console.WriteLine( "{0} status {1} {2} {3} {4}", order.ClOrdID, order.OrdStatus, 
      //	OutStandingOrdersExist, ordersSubmitted.Count, ordersPartiallyFilled.Count );
    }

    void order_ExecutionReport( object sender, ExecutionReportEventArgs args ) {

      SingleOrder order = sender as SingleOrder;
      ExecutionReport report = args.ExecutionReport;

      //Console.WriteLine("Execution report type : " + report.ExecType);

      if (report.ExecType == ExecType.Fill || report.ExecType == ExecType.PartialFill) {
        //Console.WriteLine("Fill report, average fill price = {0}@{1:#.00}", report.OrderQty, report.AvgPx);
        //Console.WriteLine( "*** {0} Report {1} cum {2} leaves {3} last {4} total {5} ",
        //	instrument.Symbol, report.OrdStatus, report.CumQty, report.LeavesQty, report.LastQty, report.OrderQty );
        switch (order.Side) {
          case Side.Buy:
            PositionFilled += (int)Math.Round(report.LastQty);
            break;
          case Side.Sell:
            PositionFilled -= (int)Math.Round(report.LastQty);
            break;
        }
      }
      if (report.ExecType == ExecType.Fill) {
        if (ordersTag.ContainsKey(order.ClOrdID)) {
          EOrderTag tag = (EOrderTag)ordersTag[order.ClOrdID];
          if (EOrderTag.Entry == tag) {
            AvgEntryPrice = report.AvgPx;
            switch (EntrySignal) {
              case ESignal.Long:
                HardStopPrice = AvgEntryPrice - HardStopDelta;  // this may not work if we have multiple partial fills
                SoftStopPrice = AvgEntryPrice - TrailingStopDelta;
                break;
              case ESignal.Short:
                HardStopPrice = AvgEntryPrice + HardStopDelta;  // this may not work if we have multiple partial fills
                SoftStopPrice = AvgEntryPrice + TrailingStopDelta;
                break;
            }
          }
        }
      }
    }

    void OnStrategyStop( object o, EventArgs e ) {
      //Console.WriteLine( "{0} TransactionSet StrategyStop", instrument.Symbol );
      UpdateSignal(ESignal.Exit);
      ClearEvents();
    }

    private void OnUpdateQuote( object source, Quote quote ) {
      //Console.WriteLine( "In UpdateQuote" );
      this.quote = quote;
      trip.UpdateQuote(quote);
      CheckSoftStop();
      //Console.WriteLine( "updatequote {0} {1} {2} {3}", State, OutStandingOrdersExist, ordersSubmitted.Count, ordersPartiallyFilled.Count );
      if (EState.CleanUp == State && !OutStandingOrdersExist) {
        //if ( !OutStandingOrdersExist ) {
        //Console.WriteLine( "transaction set final clean up" );
        ClearEvents();
        eventholder.SignalStatus(this, true);
        State = EState.Done;
      }
    }

    void ClearEvents() {
      if (UpdateQuoteActive) {
        eventholder.OnUpdateQuote -= OnUpdateQuote;
        UpdateQuoteActive = false;
      }
      if (OnStrategyStopEventActive) {
        eventholder.OnStrategyStop -= OnStrategyStop;
        OnStrategyStopEventActive = false;
      }
    }
  }

  #endregion TransactionSet

  #region RunningMinMax

  public class PointStat {

    public int PriceCount = 0;  // # of objects at this price point
    public int PriceVolume = 0;  // how much volume at this price point

    public PointStat() {
      PriceCount++;
    }
  }

  public class RunningMinMax {

    // basically keeps track of Min and Max value over selected duration

    public double Max = 0;
    public double Min = 0;

    protected SortedList slPoints;  // holds array of stats per price point
    protected int PointCount = 0;

    public RunningMinMax() {
      slPoints = new SortedList(500);
    }

    protected virtual void AddPoint( double val ) {
      //qPoints.Enqueue( val );
      if (slPoints.ContainsKey(val)) {
        PointStat ps = (PointStat)slPoints[val];
        ps.PriceCount++;
      }
      else {
        slPoints.Add(val, new PointStat());
        Max = (double)slPoints.GetKey(slPoints.Count - 1);
        Min = (double)slPoints.GetKey(0);
      }
      PointCount++;
    }

    protected virtual void RemovePoint( double val ) {
      //double t = (double) qPoints.Dequeue();
      if (slPoints.ContainsKey(val)) {
        PointStat ps = (PointStat)slPoints[val];
        ps.PriceCount--;
        if (0 == ps.PriceCount) {
          slPoints.Remove(val);
          if (0 < slPoints.Count) {
            Min = (double)slPoints.GetKey(0);
            Max = (double)slPoints.GetKey(slPoints.Count - 1);
            //Console.Write( "  Min {0:#.00} Max {1:#.00}", Min, Max );
          }
        }
        PointCount--;
      }
      else {
        throw new Exception("slPoints doesn't have a point to remove");
      }
    }

  }

  #endregion RunningMinMax

  #region AccumulationGroup

  #region RunningStats

  public class RunningStats {

    public double b2 = 0; // acceleration
    public double b1 = 0; // slope
    public double b0 = 0; // offset

    public double meanY;

    protected double SumXX = 0.0;
    protected double SumX = 0.0;
    protected double SumXY = 0.0;
    protected double SumY = 0.0;
    protected double SumYY = 0.0;

    protected double Sxx;
    protected double Sxy;
    protected double Syy;

    protected double SST;
    protected double SSR;
    protected double SSE;

    protected double RR;
    protected double R;

    protected double SD;

    protected int Xcnt = 0;

    private bool CanCalcSlope = false;

    public RunningStats() {
    }

    protected void Add( double x, double y ) {
      //Console.WriteLine( "add,{0},{1:#.00},{2:#.000}", dsSlope.Name, val, x );
      SumXX += x * x;
      SumX += x;
      SumXY += x * y;
      SumY += y;
      SumYY += y * y;
      Xcnt++;
    }

    protected void Remove( double x, double y ) {
      //Console.WriteLine( "rem,{0},{1:#.00},{2:#.000}", dsSlope.Name, val, x );
      SumXX -= x * x;
      SumX -= x;
      SumXY -= x * y;
      SumY -= y;
      SumYY -= y * y;
      Xcnt--;

      CanCalcSlope = true;
    }

    protected virtual void CalcStats() {

      double oldb1 = b1;

      Sxx = SumXX - SumX * SumX / Xcnt;
      Sxy = SumXY - SumX * SumY / Xcnt;
      Syy = SumYY - SumY * SumY / Xcnt;

      SST = Syy;
      SSR = Sxy * Sxy / Sxx;
      SSE = SST - SSR;

      RR = SSR / SST;
      R = Sxy / Math.Sqrt(Sxx * Syy);

      SD = Math.Sqrt(Syy / (Xcnt - 1));

      meanY = SumY / Xcnt;

      if (CanCalcSlope) {
        b1 = Sxy / Sxx;
      }
      else {
        b1 = 0;
      }
      b0 = (1 / Xcnt) * (SumY - b1 * SumX);
      b2 = b1 - oldb1;  // *** do this differently
    }
  }

  #endregion RunningStats

  #region Accumulation

  public class ValueAtTime {
    public DateTime DateTime;
    public double Value;

    public ValueAtTime( DateTime DateTime, double Value ) {
      this.DateTime = DateTime;
      this.Value = Value;
    }
  }

  public class Accumulation : RunningStats {

    Color color;

    protected int WindowSizeCount = 0;
    protected TimeSpan tsWindow;
    protected long firstTimeTick = 0;  // use as offset, or bias for time calc in 

    public DoubleSeries dsSlope;
    public DoubleSeries dsRR;
    public DoubleSeries dsAvg;

    private DateTime dtLast;

    protected LinkedList<ValueAtTime> values;

    protected Accumulation enclosingAccumulation = null;

    public Accumulation EnclosingAccumulation {
      set { enclosingAccumulation = value; }
    }

    public Accumulation(
      string Name, Color color, int WindowSizeTime, int WindowSizeCount ) {

      this.color = color;

      tsWindow = new TimeSpan(0, 0, WindowSizeTime); // WindowSize is in seconds
      this.WindowSizeCount = WindowSizeCount;

      values = new LinkedList<ValueAtTime>();

      dsSlope = new DoubleSeries("b1 " + Name);
      dsSlope.Color = color;

      dsRR = new DoubleSeries("rr " + Name);
      dsRR.Color = color;

      dsAvg = new DoubleSeries("avg " + Name);
      dsAvg.SecondColor = Color.Purple;
      dsAvg.Color = color;
    }

    public virtual void Add( DateTime dt, double val ) {
      values.AddLast(new ValueAtTime(dt, val));
      dtLast = dt;
      double t = (double)(dt.Ticks - firstTimeTick) / ((double)TimeSpan.TicksPerSecond);
      Add(t, val);
    }

    private void Remove( DateTime dt, double val ) {
      double t = (double)(dt.Ticks - firstTimeTick) / ((double)TimeSpan.TicksPerSecond);
      Remove(t, val);
    }

    protected override void CalcStats() {
      base.CalcStats();
      //dsAccel.Add( dtLast, b2 * 10000.0 );
      dsSlope.Add(dtLast, b1);
      dsRR.Add(dtLast, RR);
      //dsSD.Add( dtLast, SD );
      dsAvg.Add(dtLast, meanY);
    }

    protected void CheckWindow() {
      bool Done;
      if (0 < values.Count) {
        DateTime dtPurgePrior = dtLast - tsWindow;

        // Time based decimation
        Done = false;
        while (!Done) {
          ValueAtTime vat = values.First.Value;
          DateTime dtFirst = vat.DateTime;
          if (vat.DateTime < dtPurgePrior) {
            Remove(vat.DateTime, vat.Value);
            values.RemoveFirst();
            if (0 == values.Count) Done = true;
          }
          else {
            Done = true;
          }
        }

        // Count based decimation
        while (WindowSizeCount < values.Count) {
          ValueAtTime vat = values.First.Value;
          Remove(vat.DateTime, vat.Value);
          values.RemoveFirst();
        }
      }
    }
  }

  public class AccumulateValues : Accumulation {

    public AccumulateValues( string Name, Color color, int WindowSizeTime )
      :
      base(Name, color, WindowSizeTime, 100000) {
    }

    public override void Add( DateTime dt, double val ) {
      if (0 == values.Count) {
        firstTimeTick = dt.Ticks;
      }
      base.Add(dt, val);
      CheckWindow();
      CalcStats();
    }
  }

  public class AccumulateQuotes : Accumulation {

    //private QuoteArray quotes;
    private TimeSpan ms;
    private DateTime dtUnique;

    public bool CalcTrade;

    protected double BBMultiplier;
    public DoubleSeries dsBBUpper;
    public DoubleSeries dsBBLower;
    public DoubleSeries dsB;
    public DoubleSeries dsBandwidth;

    private AccumulateValues slopeAvg;
    public DoubleSeries dsSlopeAvg;
    private AccumulateValues accelAvg;
    public DoubleSeries dsAccelAvg;

    private double m_SlopeAvgScaleMin = 0;
    private double m_SlopeAvgScaleMax = 0;

    private double m_bbwMin = 0;
    private double m_bbwMax = 0;

    private double m_AccelAvgScaleMin = 0;
    private double m_AccelAvgScaleMax = 0;

    public AccumulateQuotes( string Name, int WindowSizeTime, int WindowSizeCount,
        double BBMultiplier, bool CalcTrade, Color color )
      :
      base(Name, color, WindowSizeTime, WindowSizeCount) {

      this.CalcTrade = CalcTrade;
      ms = new TimeSpan(0, 0, 0, 0, 1);

      this.BBMultiplier = BBMultiplier;

      // see page 157 in Bollinger on Bollinger Bands

      slopeAvg = new AccumulateValues("slope(avg) " + Name, color, WindowSizeTime / 4);
      //dsSlopeAvg = slopeAvg.dsSlope;
      dsSlopeAvg = new DoubleSeries("slope(avg) " + Name);
      dsSlopeAvg.Color = color;
      slopeAvg.dsSlope.ItemAdded += new ItemAddedEventHandler(slopeItemAddedEventHandler);

      accelAvg = new AccumulateValues("accel(avg) " + Name, color, WindowSizeTime / 16);
      dsAccelAvg = new DoubleSeries("accel(avg) " + Name);
      dsAccelAvg.Color = color;
      accelAvg.dsSlope.ItemAdded += new ItemAddedEventHandler(accelItemAddedEventHandler);

      dsBBUpper = new DoubleSeries("bbu " + Name);
      dsBBUpper.Color = color;

      dsBBLower = new DoubleSeries("bbl " + Name);
      dsBBLower.Color = color;

      dsB = new DoubleSeries("%b " + Name);
      dsB.Color = color;

      dsBandwidth = new DoubleSeries("bbw " + Name);
      dsBandwidth.Color = color;

    }

    void slopeItemAddedEventHandler( object sender, DateTimeEventArgs e ) {
      double val = (slopeAvg.dsSlope.Last - m_SlopeAvgScaleMin) / (m_SlopeAvgScaleMax - m_SlopeAvgScaleMin);
      dsSlopeAvg.Add(e.DateTime, val);
      accelAvg.Add(e.DateTime, val);
    }

    void accelItemAddedEventHandler( object sender, DateTimeEventArgs e ) {
      double val = accelAvg.dsSlope.Last;
      double tmp = (val - m_AccelAvgScaleMin) / (m_AccelAvgScaleMax - m_AccelAvgScaleMin);
      tmp = Math.Min(tmp, 1.25);
      tmp = Math.Max(tmp, -0.25);
      dsAccelAvg.Add(e.DateTime, tmp);
    }

    public double SlopeAvgScaleMin {
      get { return m_SlopeAvgScaleMin; }
      set { m_SlopeAvgScaleMin = value; }
    }

    public double SlopeAvgScaleMax {
      get { return m_SlopeAvgScaleMax; }
      set { m_SlopeAvgScaleMax = value; }
    }

    public double AccelAvgScaleMin {
      get { return m_AccelAvgScaleMin; }
      set { m_AccelAvgScaleMin = value; }
    }

    public double AccelAvgScaleMax {
      get { return m_AccelAvgScaleMax; }
      set { m_AccelAvgScaleMax = value; }
    }

    public double bbwMin {
      get { return m_bbwMin; }
      set { m_bbwMin = value; }
    }

    public double bbwMax {
      get { return m_bbwMax; }
      set { m_bbwMax = value; }
    }

    public void Add( Quote quote ) {
      if (0 == values.Count) {
        dtUnique = quote.DateTime;
        firstTimeTick = quote.DateTime.Ticks;
      }
      else {
        dtUnique = (quote.DateTime > dtUnique) ? quote.DateTime : dtUnique + ms;
      }
      Add(dtUnique, quote.Bid);
      Add(dtUnique, quote.Ask);

      //Console.WriteLine( "{0} Added {1:#.00} {2:#.00} {3:#.00} {4}", dt, val, Min, Max, dsPoints.Count );

      CheckWindow();
      CalcStats();

      if (!double.IsNaN(b1) && !double.IsPositiveInfinity(b1) && !double.IsNegativeInfinity(b1)) {

        slopeAvg.Add(dtUnique, meanY);

        double upper = meanY + BBMultiplier * SD;
        double lower = meanY - BBMultiplier * SD;
        dsBBUpper.Add(dtUnique, upper);
        dsBBLower.Add(dtUnique, lower);

        double tmp = (quote.Bid + quote.Ask) / 2;
        double avgquote = 0;
        if (tmp == meanY) {
          avgquote = tmp;
        }
        else {
          if (tmp > meanY) avgquote = quote.Ask;
          if (tmp < meanY) avgquote = quote.Bid;
        }
        dsB.Add(dtUnique, (avgquote - lower) / (upper - lower));
        double bw = 1000.0 * (upper - lower) / meanY;
        dsBandwidth.Add(dtUnique, bw / m_bbwMax);
      }
      //Console.WriteLine( "{0} Added {1} {2} {3} {4} {5}", dtUnique, Xcnt, b1, b0, R, dsSlope.Name );
    }

  }

  #endregion Accumulation

  #endregion AccumulationGroup

  #region Pattern Analysis

  public enum EPatternType { Uninteresting, UpTrend, DownTrend, HeadAndShoulders, InvertedHeadAndShoulders, Triangle, Broadening };

  public class PatternInfo {


    public string PatternId;
    public EPatternType PatternType;

    public PatternInfo( string PatternId, EPatternType PatternType ) {
      this.PatternId = PatternId;
      this.PatternType = PatternType;
    }
  }

  public class MerrillPattern {

    // page 94 in Bollinger Bands

    static Hashtable htPatterns;

    static MerrillPattern() {

      htPatterns = new Hashtable(32);

      htPatterns["21435"] = new PatternInfo("M1", EPatternType.DownTrend);
      htPatterns["21534"] = new PatternInfo("M2", EPatternType.InvertedHeadAndShoulders);
      htPatterns["31425"] = new PatternInfo("M3", EPatternType.DownTrend);
      htPatterns["31524"] = new PatternInfo("M4", EPatternType.InvertedHeadAndShoulders);
      htPatterns["32415"] = new PatternInfo("M5", EPatternType.Broadening);
      htPatterns["32514"] = new PatternInfo("M6", EPatternType.InvertedHeadAndShoulders);
      htPatterns["41325"] = new PatternInfo("M6", EPatternType.Uninteresting);
      htPatterns["41523"] = new PatternInfo("M8", EPatternType.InvertedHeadAndShoulders);
      htPatterns["42315"] = new PatternInfo("M9", EPatternType.Uninteresting);
      htPatterns["42513"] = new PatternInfo("M10", EPatternType.InvertedHeadAndShoulders);
      htPatterns["43512"] = new PatternInfo("M11", EPatternType.InvertedHeadAndShoulders);
      htPatterns["51324"] = new PatternInfo("M12", EPatternType.Uninteresting);
      htPatterns["51423"] = new PatternInfo("M13", EPatternType.Triangle);
      htPatterns["52314"] = new PatternInfo("M14", EPatternType.Uninteresting);
      htPatterns["52413"] = new PatternInfo("M15", EPatternType.UpTrend);
      htPatterns["53412"] = new PatternInfo("M16", EPatternType.UpTrend);

      htPatterns["13254"] = new PatternInfo("W1", EPatternType.DownTrend);
      htPatterns["14253"] = new PatternInfo("W2", EPatternType.DownTrend);
      htPatterns["14352"] = new PatternInfo("W3", EPatternType.Uninteresting);
      htPatterns["15243"] = new PatternInfo("W4", EPatternType.Triangle);
      htPatterns["15342"] = new PatternInfo("W5", EPatternType.Uninteresting);
      htPatterns["23154"] = new PatternInfo("W6", EPatternType.HeadAndShoulders);
      htPatterns["24153"] = new PatternInfo("W7", EPatternType.HeadAndShoulders);
      htPatterns["24351"] = new PatternInfo("W8", EPatternType.Uninteresting);
      htPatterns["25143"] = new PatternInfo("W9", EPatternType.HeadAndShoulders);
      htPatterns["25341"] = new PatternInfo("W10", EPatternType.Uninteresting);
      htPatterns["34152"] = new PatternInfo("W11", EPatternType.HeadAndShoulders);
      htPatterns["34251"] = new PatternInfo("W12", EPatternType.Broadening);
      htPatterns["35142"] = new PatternInfo("W13", EPatternType.HeadAndShoulders);
      htPatterns["35241"] = new PatternInfo("W14", EPatternType.UpTrend);
      htPatterns["45132"] = new PatternInfo("W15", EPatternType.HeadAndShoulders);
      htPatterns["45231"] = new PatternInfo("W16", EPatternType.UpTrend);

      foreach (string key in htPatterns.Keys) {
        if (!key.Contains("1")) Console.WriteLine("{0} missing 1", key);
        if (!key.Contains("2")) Console.WriteLine("{0} missing 2", key);
        if (!key.Contains("3")) Console.WriteLine("{0} missing 3", key);
        if (!key.Contains("4")) Console.WriteLine("{0} missing 4", key);
        if (!key.Contains("5")) Console.WriteLine("{0} missing 5", key);
      }
    }

    public MerrillPattern() {
    }

    public string Classify( double p1, double p2, double p3, double p4, double p5 ) {
      SortedList sl = new SortedList(5);

      bool ok = true;
      try {
        sl.Add(p1, "1");
        sl.Add(p2, "2");
        sl.Add(p3, "3");
        sl.Add(p4, "4");
        sl.Add(p5, "5");
      }
      catch {
        ok = false;
      }
      if (ok) {
        string key = (string)sl.GetByIndex(4)
          + (string)sl.GetByIndex(3)
          + (string)sl.GetByIndex(2)
          + (string)sl.GetByIndex(1)
          + (string)sl.GetByIndex(0);
        if (htPatterns.ContainsKey(key)) {
          PatternInfo pi = (PatternInfo)htPatterns[key];
          Console.WriteLine("{0} Pattern {1} {2}", Clock.Now, pi.PatternId, pi.PatternType);
          return pi.PatternId;
        }
        else {
          //Console.WriteLine( "{0} Pattern {1} not found", Clock.Now, key );
          return "";
        }
      }
      else {
        return "";
      }
    }

    public string ClassifyDoubleSeriesEnd( DoubleSeries ds ) {
      if (ds.Count >= 5) {
        return Classify(ds.Ago(4), ds.Ago(3), ds.Ago(2), ds.Ago(1), ds.Last);
      }
      else return "";
    }
  }

  #endregion Pattern Analysis

  #region Fuzzy Logic

  // Fuzzy Logic based upon page 2-29 (59) of MatLabs Fuzzy Tutorial
  // "the controller for the injector aperture not only processes the termperature and pressure but 
  // considers the change in the temperature and pressure since the last sensor reading and also
  // the load on the turbine in the form of the rotr RPM rate.  In fact in most control systems, it is the change 
  // that provides input into the fuzzy logic controllor.  pg 421-422 Fuzzy Systems Handbook

  public class Point {
    double x;
    double y;

    public double X {
      get { return x; }
    }

    public double Y {
      get { return y; }
    }

    public Point() {
      x = 0;
      y = 0;
    }

    public Point( double x, double y ) {
      this.x = x;
      this.y = y;
    }
  }

  public class MembershipFunction {

    public MembershipFunction() {
    }
  }

  public class mfTwoPoint : MembershipFunction {
    Point left;
    Point right;

    public mfTwoPoint( Point left, Point right ) {
      this.left = left;
      this.right = right;
    }
  }

  public class mfThreePoint : MembershipFunction {

    Point left;
    Point middle;
    Point right;

    public mfThreePoint( Point left, Point middle, Point right ) {
      this.left = left;
      this.middle = middle;
      this.right = right;
    }
  }

  public class mfFourPoint : MembershipFunction {

    Point left;
    Point midleft;
    Point midright;
    Point right;

    public mfFourPoint( Point left, Point midleft, Point midright, Point right ) {
      this.left = left;
      this.midleft = midleft;
      this.midright = midright;
      this.right = right;
    }
  }

  public class FuzzyInput {

    MembershipFunction mf;

    public FuzzyInput( MembershipFunction mf ) {
      this.mf = mf;
    }
  }

  public class FuzzyRule {

    enum Efo { And, Or }; // fuzzy operation

    ArrayList alFuzzyInputs;
    Efo fo = Efo.And;
    MembershipFunction fuzzyresult;
    MembershipFunction implication;

    public FuzzyRule() {

      alFuzzyInputs = new ArrayList(10);
    }
  }

  public class FuzzyAggregation {

    ArrayList alFuzzyRules;

    public FuzzyAggregation() {
      alFuzzyRules = new ArrayList(10);
    }

    public double Centroid() {
      return 0;
    }
  }

  public class FuzzyLogic {
    FuzzyInput fiLowerBollinger = new FuzzyInput(
        new mfTwoPoint(
          new Point(0, 1), new Point(0.5, 0))); // at %b = 0 at lower, at %b = .5, not at lower
    FuzzyInput fiMiddleBollinger = new FuzzyInput(
      new mfThreePoint(
      new Point(0, 0), new Point(0.5, 1), new Point(1, 0))); // x < 0: no middle, 0 < x < 1: middle, x > 1: no middle
    FuzzyInput fiUpperBollinger = new FuzzyInput(
      new mfTwoPoint(
        new Point(0.5, 0), new Point(1, 1))); // at %b = 0.5 no upper, at %b = 1, at upper
  }

  #endregion

  #region TVI

  public class TVI : DoubleSeries {

    private double MTV;  // Minimum Tick Value
    private double LastPrice;
    private enum EState { init, first, go };
    private EState state = EState.init;
    private enum EDirection { unknown, accumulate, distribute };
    private EDirection direction = EDirection.unknown;
    private double accum = 0;

    public TVI( double MinimumTickValue ) {
      MTV = MinimumTickValue;
    }

    public TVI( double MinimumTickValue, string Name )
      : base(Name) {
      MTV = MinimumTickValue;
    }

    public TVI( double MinimumTickValue, string Name, string Title )
      : base(Name, Title) {
      MTV = MinimumTickValue;
    }

    public override void Add( DateTime DateTime, double Data ) {
      throw new Exception("Can not use TVI.Add( dt, data )");
    }

    public void Add( Trade trade, Quote quote ) {
      double midpoint = (quote.Bid + quote.Ask) / 2;
      if (trade.Price == midpoint) {
        switch (direction) {
          case EDirection.accumulate:
            accum += trade.Size;
            break;
          case EDirection.distribute:
            accum -= trade.Size;
            break;
        }
      }
      else {
        if (trade.Price > midpoint) {
          accum += trade.Size;
          direction = EDirection.accumulate;
        }
        if (trade.Price < midpoint) {
          accum -= trade.Size;
          direction = EDirection.distribute;
        }
      }
      base.Add(trade.DateTime, accum);
    }

    public void Add( Trade trade ) {
      // assumes additions in chronological order
      double change = 0;

      switch (state) {
        case EState.go:
          change = trade.Price - LastPrice;
          if (Math.Abs(change) > MTV) {
            direction = change > 0 ? EDirection.accumulate : EDirection.distribute;
          }
          switch (direction) {
            case EDirection.accumulate:
              accum += trade.Size;
              break;
            case EDirection.distribute:
              accum -= trade.Size;
              break;
          }
          base.Add(trade.DateTime, accum);
          break;
        case EState.first:
          change = trade.Price - LastPrice;
          if (Math.Abs(change) > MTV) {
            if (change > 0) {
              accum = trade.Size;
              direction = EDirection.accumulate;
            }
            if (change < 0) {
              accum = -trade.Size;
              direction = EDirection.distribute;
            }
            base.Add(trade.DateTime, accum);
            state = EState.go;
          }
          break;
        case EState.init:
          state = EState.first;
          break;

      }

      LastPrice = trade.Price;
    }
  }

  #endregion TVI

  [StrategyComponent("{44913622-f941-41e2-adde-d905585e847f}", ComponentType.ATSComponent, Name = "atscScalping", Description = "")]
  public class atscScalping : ATSComponent {
    MerrillPattern mp;

    double dayhi;

    double daylo;
    double daycl;

    double weekhi;
    double weeklo;
    double weekcl;

    double monhi;
    double monlo;
    double moncl;

    DoubleSeries dsOpen;

    DoubleSeries dsDayCl;
    DoubleSeries dsDayR3;
    DoubleSeries dsDayR2;
    DoubleSeries dsDayR1;
    DoubleSeries dsDayPv;
    DoubleSeries dsDayS1;
    DoubleSeries dsDayS2;
    DoubleSeries dsDayS3;

    DoubleSeries dsWeekCl;
    DoubleSeries dsWeekR3;
    DoubleSeries dsWeekR2;
    DoubleSeries dsWeekR1;
    DoubleSeries dsWeekPv;
    DoubleSeries dsWeekS1;
    DoubleSeries dsWeekS2;
    DoubleSeries dsWeekS3;

    DoubleSeries dsMonCl;
    DoubleSeries dsMonR3;
    DoubleSeries dsMonR2;
    DoubleSeries dsMonR1;
    DoubleSeries dsMonPv;
    DoubleSeries dsMonS1;
    DoubleSeries dsMonS2;
    DoubleSeries dsMonS3;

    DoubleSeries dsSma20Day;
    DoubleSeries dsSma200Day;

    double open;

    double dayR3;
    double dayR2;
    double dayR1;
    double dayPv;
    double dayS1;
    double dayS2;
    double dayS3;

    double weekR3;
    double weekR2;
    double weekR1;
    double weekPv;
    double weekS1;
    double weekS2;
    double weekS3;

    double monR3;
    double monR2;
    double monR1;
    double monPv;
    double monS1;
    double monS2;
    double monS3;

    double sma20day;
    double sma200day;

    QuoteArray qa1;
    DoubleSeries dsLine;
    int cntQuotes = 0;
    int cntTrades = 0;
    TimeSpan ms;
    bool CanTrade = false;  // performance window doesn't work properly if trades generated before trade enters system

    DoubleSeries dsSignal;
    double LastSignalPrice = 0.0;

    DoubleSeries dsBid;
    DoubleSeries dsBidVolume;
    DoubleSeries dsAsk;
    DoubleSeries dsAskVolume;
    DoubleSeries dsQuotesPerSecond;
    int QuotesPerSecondCount;
    TimeSpan tsQuoteSpan;

    BarSeries bars;

    DoubleSeries dsII;
    DoubleSeries dsAD;

    DoubleSeries dsTrade;
    DoubleSeries dsTradeVolume;
    DoubleSeries dsTradesPerSecond;
    int TradesPerSecondCount;
    TimeSpan tsTradeSpan;
    int TradeVolumePerSecond;
    DoubleSeries dsVolumePerSecond;
    DoubleSeries dsVolumePerRange;
    double TradeHigh;
    double TradeLow;

    TVI tvi;

    SortedList slVolumePerPrice;

    AccumulateQuotes accum;
    int intWindowSizeSec = 24;
    int intWindowSizeCnt = 100000;

    SingleOrder marketOrder;
    SingleOrder limitOrder;
    int ActiveLimitOrderCount = 0;
    int intOrderSize = 100;
    int intMaxScaleIn = 100;
    int intScaleInSize = 100;
    double dblScaleInIncrement = 0.05;

    DateTime dtCurrentDay;
    //DailySeries daily;

    DoubleSeries dsZero;
    DoubleSeries dsHalf;
    DoubleSeries dsOne;

    TimeSpan tsStart;
    TimeSpan tsStop;

    ATSStrategy strat;
    ATSMetaStrategy metastrat;
    MarketManager mm;

    GPStrategy gpstrategy;

    double dblStop;

    bool StopSignalled = false;
    double StopDelta = 1.75;

    int PositionRequested = 0;
    int PositionSoFar = 0;

    enum PositionState {
      Empty, LongContinue, LongReversed, ShortContinue, ShortReversed
    }

    PositionState state;

    AccumulateQuotes[] rAccum;

    double pivotHigh;
    double pivotLow;
    double pivotClose;
    bool pivotDraw = false;
    DoubleSeries dsPivot;
    DoubleSeries dsPivotR1;
    DoubleSeries dsPivotR2;
    DoubleSeries dsPivotS1;
    DoubleSeries dsPivotS2;
    double pivot;
    double pivotR1;
    double pivotR2;
    double pivotS1;
    double pivotS2;

    DateTime dtLastDirectionSignal;
    int intExitWait = 10000000;

    //ArrayList alRoundTrips;
    RoundTrip latestRoundTrip;
    Quote latestQuote;

    DateTime dtLastTrigger = new DateTime(0);
    enum Direction { None, Up, Down };
    Direction LastDirection = Direction.None;
    int DirectionCount = 0;
    double PriceAtDirectionChange = 0;
    double PredictedPriceJump = 0;

    TransactionSet trans;

    TransactionSetEventHolder eventholder;

    double dblPatternDelta = 0.30; // pt1 becomes new anchor when abs(pt0-pt1)>delta
    int szPDD;
    double PatternPt0; // pattern end point, drags pt1 away from anchor, but can retrace at will
    double PatternPt1; // pattern mid point, can only move away from anchor point
    DateTime dtPatternPt1;  // when it was last encountered
    DoubleSeries dsPattern;
    enum EPatternState { init, start, down, up };
    EPatternState PatternState;
    // pt0, pt1 are set when first delta has been reached
    int[] rPatternDeltaDistance;
    DoubleSeries dsPt0Ratio;
    double cntNewUp;
    double cntNewDown;
    DoubleSeries dsSellDecisionPoint;
    DoubleSeries dsBuyDecisionPoint;
    bool bSellDecisionPointSet;
    bool bBuyDecisionPointSet;

    int MinDir = 7;
    int MinDur = 1000; // minimum duration to wait to confirm delta (milliseconds)
    int MinCnt = 5;  // number of ever increasing changes to trigger
    int BarWidth = 6;  // seconds

    //double hardstop = 0.35;
    double hardstop = 0.60;
    double trailingstop = 1;

    OneUnified.IQFeed.OrderBook ob;
    TimeSpan LastOrderBookSample;
    TimeSpan OrderBookSampleInterval;
    DoubleSeries dsBidDepth;
    DoubleSeries dsAskDepth;
    DoubleSeries dsDepth;
    DoubleSeries dsWP1ToStep;

    bool bCanUseForms;
    bool bFormsLoaded;
    frmOrderBookView1 l2vu1;
    frmOrderBookView2 l2vu2;

    int l2maxsteps = 8;
    int l2maxstepsvu = 4;
    DoubleSeries[] l2Bid;
    DoubleSeries[] l2Ask;
    DoubleSeries BidBig;
    DoubleSeries AskBig;
    DoubleSeries BidBigNum;
    DoubleSeries AskBigNum;
    bool bDisplayDepth;

    DoubleSeries dsPressureBar;
    DoubleSeries dsPressureBarNormalized;

    DateTime dtRunningStart;

    #region Strategy Parameters

    [Category("Parameter"), Description("Pattern Delta (cents)")]
    //[OptimizationParameter(10, 100, 10)]
    public double PatternDelta {
      get { return dblPatternDelta; }
      set { dblPatternDelta = value; }
    }

    [Category("Parameter"), Description("Max Scale In")]
    //[OptimizationParameter(100, 1000, 100)]
    public int ScaleInMax {
      get { return intMaxScaleIn; }
      set { intMaxScaleIn = value; }
    }

    [Category("Parameter"), Description("Scale In Increment")]
    //[OptimizationParameter(100, 1000, 100)]
    public double ScaleInIncrement {
      get { return dblScaleInIncrement; }
      set { dblScaleInIncrement = value; }
    }

    [Category("Parameter"), Description("Scale In Size")]
    //[OptimizationParameter(100, 1000, 100)]
    public int ScaleInSize {
      get { return intScaleInSize; }
      set { intScaleInSize = value; }
    }

    [Category("Parameter"), Description("Window (sec)")]
    //[OptimizationParameter(20, 36, 2)]
    public int WindowTime {
      get { return intWindowSizeSec; }
      set { intWindowSizeSec = value; }
    }

    [Category("Parameter"), Description("Window (sec)")]
    //[OptimizationParameter(20, 36, 2)]
    public int WindowCount {
      get { return intWindowSizeCnt; }
      set { intWindowSizeCnt = value; }
    }

    [Category("Pivot Point"), Description("Draw")]
    public bool PivotDraw {
      get { return pivotDraw; }
      set { pivotDraw = value; }
    }

    [Category("Pivot Point"), Description("High")]
    public double PivotHigh {
      get { return pivotHigh; }
      set { pivotHigh = value; }
    }

    [Category("Pivot Point"), Description("Low")]
    public double PivotLow {
      get { return pivotLow; }
      set { pivotLow = value; }
    }

    [Category("Pivot Point"), Description("Close")]
    public double PivotClose {
      get { return pivotClose; }
      set { pivotClose = value; }
    }

    [Category("Indicator"), Description("MinDur(ms)")]
    public int MinimumDuration {
      get { return MinDur; }
      set { MinDur = value; }
    }

    [Category("Indicator"), Description("MinDir")]
    [OptimizationParameter(3, 8, 1)]
    public int MinimumDirection {
      get { return MinDir; }
      set { MinDir = value; }
    }

    [Category("Indicator"), Description("MinCnt")]
    [OptimizationParameter(3, 6, 1)]
    public int MinimumCount {
      get { return MinCnt; }
      set { MinCnt = value; }
    }

    [Category("Parameter"), Description("Exit Wait (ms)")]
    public int ExitWait {
      get { return intExitWait; }
      set { intExitWait = value; }
    }

    #endregion Strategy Parameters

    public atscScalping()
      : base() {
      gpstrategy = new GPStrategy();
    }

    public bool IsGUIFrozen {

      get {
        Type type = Type.GetType("QuantDeveloper.Developer, QuantDeveloper");

        FieldInfo field = type.GetField("IsGUIFrozen", BindingFlags.Public | BindingFlags.Static);

        return (bool)field.GetValue(null);
      }
    }

    public override void Init() {

      strat = base.Strategy;
      metastrat = strat.ATSMetaStrategy;
      mm = strat.MarketManager;

      dtRunningStart = DateTime.Now;

      mp = new MerrillPattern();

      RoundTrip.id = 0;

      TradeDB db1 = new TradeDB();
      db1.Open();

      string sql = "select dayhi, daylo, daycl, weekhi, weeklo, weekcl, monhi, monlo, moncl, sma20day, sma200day"
        + " from totrade where tradesystem='gap' and symbol = '" + Instrument.Symbol + "'";
      SqlCommand cmd = new SqlCommand(sql, db1.Connection);

      SqlDataReader dr;
      dr = cmd.ExecuteReader();
      if (dr.HasRows) {
        while (dr.Read()) {
          dayhi = dr.GetDouble(0);
          daylo = dr.GetDouble(1);
          daycl = dr.GetDouble(2);
          weekhi = dr.GetDouble(3);
          weeklo = dr.GetDouble(4);
          weekcl = dr.GetDouble(5);
          monhi = dr.GetDouble(6);
          monlo = dr.GetDouble(7);
          moncl = dr.GetDouble(8);
          sma20day = dr.GetDouble(9);
          sma200day = dr.GetDouble(10);
        }
      }

      dr.Close();
      db1.Close();

      dayPv = (dayhi + daylo + daycl) / 3;
      dayR1 = 2 * dayPv - daylo;
      dayR2 = dayPv + (dayhi - daylo);
      dayR3 = dayR1 + (dayhi - daylo);
      dayS1 = 2 * dayPv - dayhi;
      dayS2 = dayPv - (dayhi - daylo);
      dayS3 = dayS1 - (dayhi - daylo);

      weekPv = (weekhi + weeklo + weekcl) / 3;
      weekR1 = 2 * weekPv - weeklo;
      weekR2 = weekPv + (weekhi - weeklo);
      weekR3 = weekR1 + (weekhi - weeklo);
      weekS1 = 2 * weekPv - weekhi;
      weekS2 = weekPv - (weekhi - weeklo);
      weekS3 = weekS1 - (weekhi - weeklo);

      monPv = (monhi + monlo + moncl) / 3;
      monR1 = 2 * monPv - monlo;
      monR2 = monPv + (monhi - monlo);
      monR3 = monR1 + (monhi - monlo);
      monS1 = 2 * monPv - monhi;
      monS2 = monPv - (monhi - monlo);
      monS3 = monS1 - (monhi - monlo);

      dsOpen = new DoubleSeries("open ");

      dsDayCl = new DoubleSeries("day cls " + daycl.ToString("#.00"));
      dsDayPv = new DoubleSeries("day pv " + dayPv.ToString("#.00"));
      dsDayR1 = new DoubleSeries("day r1 " + dayR1.ToString("#.00"));
      dsDayR2 = new DoubleSeries("day r2 " + dayR2.ToString("#.00"));
      dsDayR3 = new DoubleSeries("day r3 " + dayR3.ToString("#.00"));
      dsDayS1 = new DoubleSeries("day s1 " + dayS1.ToString("#.00"));
      dsDayS2 = new DoubleSeries("day s2 " + dayS2.ToString("#.00"));
      dsDayS3 = new DoubleSeries("day s3 " + dayS3.ToString("#.00"));

      dsWeekCl = new DoubleSeries("week cls " + weekcl.ToString("#.00"));
      dsWeekPv = new DoubleSeries("week pv " + weekPv.ToString("#.00"));
      dsWeekR1 = new DoubleSeries("week r1" + weekR1.ToString("#.00"));
      dsWeekR2 = new DoubleSeries("week r2 " + weekR2.ToString("#.00"));
      dsWeekR3 = new DoubleSeries("week r3 " + weekR3.ToString("#.00"));
      dsWeekS1 = new DoubleSeries("week s1 " + weekS1.ToString("#.00"));
      dsWeekS2 = new DoubleSeries("week s2 " + weekS2.ToString("#.00"));
      dsWeekS3 = new DoubleSeries("week s3 " + weekS3.ToString("#.00"));

      dsMonCl = new DoubleSeries("mon cls " + moncl.ToString("#.00"));
      dsMonPv = new DoubleSeries("mon pv " + monPv.ToString("#.00"));
      dsMonR1 = new DoubleSeries("mon r1 " + monR1.ToString("#.00"));
      dsMonR2 = new DoubleSeries("mon r2 " + monR2.ToString("#.00"));
      dsMonR3 = new DoubleSeries("mon r3 " + monR3.ToString("#.00"));
      dsMonS1 = new DoubleSeries("mon s1 " + monS1.ToString("#.00"));
      dsMonS2 = new DoubleSeries("mon s2 " + monS2.ToString("#.00"));
      dsMonS3 = new DoubleSeries("mon s3 " + monS3.ToString("#.00"));

      dsSma20Day = new DoubleSeries("sma 20 " + sma20day.ToString("#.00"));
      dsSma200Day = new DoubleSeries("sma 200 " + sma200day.ToString("#.00"));

      dsOpen.Color = Color.Violet;

      dsDayCl.Color = Color.Black;
      dsDayPv.Color = Color.Green;
      dsDayR1.Color = Color.Blue;
      dsDayR2.Color = Color.Red;
      dsDayR3.Color = Color.Orange;
      dsDayS1.Color = Color.Blue;
      dsDayS2.Color = Color.Red;
      dsDayS3.Color = Color.Beige;

      dsWeekCl.Color = Color.Black;
      dsWeekPv.Color = Color.Green;
      dsWeekR1.Color = Color.Blue;
      dsWeekS1.Color = Color.Blue;

      dsMonCl.Color = Color.Black;
      dsMonPv.Color = Color.Green;
      dsMonR1.Color = Color.Blue;
      dsMonS1.Color = Color.Blue;

      dsSma20Day.Color = Color.DarkSlateBlue;
      dsSma200Day.Color = Color.DarkSeaGreen;

      Draw(dsOpen, 0);

      Draw(dsDayCl, 0);
      Draw(dsDayR3, 0);
      Draw(dsDayR2, 0);
      Draw(dsDayR1, 0);
      Draw(dsDayPv, 0);
      Draw(dsDayS1, 0);
      Draw(dsDayS2, 0);
      Draw(dsDayS3, 0);

      Draw(dsWeekCl, 0);
      Draw(dsWeekPv, 0);
      Draw(dsWeekR1, 0);
      Draw(dsWeekS1, 0);

      Draw(dsMonCl, 0);
      Draw(dsMonPv, 0);
      Draw(dsMonR1, 0);
      Draw(dsMonS1, 0);

      Draw(dsSma20Day, 0);
      Draw(dsSma200Day, 0);

      bars = new BarSeries("Bars");

      dsII = new DoubleSeries("II");
      dsII.Color = Color.Blue;
      //Draw( dsII, 8 );

      dsAD = new DoubleSeries("AD");
      dsAD.Color = Color.Blue;
      //Draw( dsAD, 9 );

      eventholder = new TransactionSetEventHolder();
      eventholder.UpdateSignalStatus += OnTransactionSetExit;

      dsZero = new DoubleSeries("Zero");
      dsZero.Color = Color.Black;

      dsHalf = new DoubleSeries("Half");
      dsHalf.Color = Color.Aquamarine;

      dsOne = new DoubleSeries("One");
      dsOne.Color = Color.Black;

      // Pivot Information
      if (pivotDraw) {
        pivot = (pivotHigh + pivotLow + pivotClose) / 3.0;
        //dsPivot = new DoubleSeries( "Pivot" );
        pivotR1 = 2.0 * pivot - pivotLow;
        pivotS1 = 2.0 * pivot - pivotHigh;
        pivotR2 = pivotR1 + pivot - pivotS1;
        pivotS2 = pivot - pivotR1 - pivotS1;
        Console.WriteLine("pivot {0} r1 {1} r2 {2} s1 {3} s2 {4}",
          pivot, pivotR1, pivotR2, pivotS1, pivotS2);
      }

      // Order Book
      ob = new OneUnified.IQFeed.OrderBook();
      //ob.InsideQuoteChangedEventHandler += new EventHandler(OrderBookQuoteEvent);
      LastOrderBookSample = new TimeSpan(0);
      OrderBookSampleInterval = new TimeSpan(0, 0, 0, 0, 2000);

      dsBidDepth = new DoubleSeries("Bid Depth");
      dsAskDepth = new DoubleSeries("Ask Depth");
      dsDepth = new DoubleSeries("Depth");
      //dsWP1ToStep = new DoubleSeries( "dsWP1ToStep" );

      l2Bid = new DoubleSeries[l2maxsteps];
      l2Ask = new DoubleSeries[l2maxsteps];
      for (int i = 0; i < l2maxstepsvu; i++) {
        l2Bid[i] = new DoubleSeries("Bid " + i.ToString());
        l2Bid[i].Color = Color.LightPink;
        //if ( i == 0 ) Draw( l2Bid[ i ], 0 );
        l2Ask[i] = new DoubleSeries("Ask " + i.ToString());
        l2Ask[i].Color = Color.LightBlue;
        //if ( i == 0 ) Draw( l2Ask[ i ], 0 );
      }
      BidBig = new DoubleSeries("Bid Big");
      BidBig.Color = Color.Red;
      Draw(BidBig, 0);
      AskBig = new DoubleSeries("Ask Big");
      AskBig.Color = Color.Blue;
      Draw(AskBig, 0);
      BidBigNum = new DoubleSeries("Bid Big Num");
      BidBigNum.Color = Color.Red;
      //Draw( BidBigNum, 4 );
      AskBigNum = new DoubleSeries("Ask Big Num");
      AskBigNum.Color = Color.Blue;
      //Draw( AskBigNum, 4 );

      dsPressureBar = new DoubleSeries("Pressure");
      dsPressureBar.Color = Color.Green;
      //Draw( dsPressureBar, 2 );
      //Draw( dsZero, 2 );
      dsPressureBarNormalized = new DoubleSeries("Pres (Norm)");
      dsPressureBarNormalized.Color = Color.LightGreen;
      //Draw( dsPressureBarNormalized, 2 );

      bDisplayDepth = false;

      dsAskDepth.Color = Color.LightBlue;
      dsBidDepth.Color = Color.LightPink;
      dsDepth.Color = Color.LightGreen;
      //dsWP1ToStep.Color = Color.Aquamarine;

      Draw(dsAskDepth, 0);
      Draw(dsDepth, 0);
      Draw(dsBidDepth, 0);
      //Draw( dsWP1ToStep, 0 );
      //Draw( dsZero, 9 );

      bCanUseForms = false;
      bFormsLoaded = false;

      // Quote Information
      qa1 = new QuoteArray();
      //Draw( qa1, 0 );
      ms = new TimeSpan(0, 0, 0, 0, 1);

      dsQuotesPerSecond = new DoubleSeries("Quotes/Sec");
      dsQuotesPerSecond.Color = Color.DarkTurquoise;
      //Draw( dsQuotesPerSecond, 4 );
      tsQuoteSpan = new TimeSpan(0);

      // Tarde Information
      dsTrade = new DoubleSeries("Trade");
      dsTrade.Color = Color.Green;
      Draw(dsTrade, 0);
      dsTradeVolume = new DoubleSeries("Trade Volume");
      dsTradeVolume.Color = Color.Green;
      dsTradeVolume.DrawStyle = EDrawStyle.Bar;
      Draw(dsTradeVolume, 1);
      dsTradesPerSecond = new DoubleSeries("Tics/Sec");
      dsTradesPerSecond.Color = Color.Green;
      //Draw( dsTradesPerSecond, 2 );
      TradesPerSecondCount = 0;
      tsTradeSpan = new TimeSpan(0);
      dsVolumePerSecond = new DoubleSeries("Vol/Sec");
      //Draw( dsVolumePerSecond, 3 );
      dsVolumePerRange = new DoubleSeries("Vol/Range");
      //Draw( dsVolumePerRange, 6 );

      slVolumePerPrice = new SortedList(1000);

      tvi = new TVI(0.05, "TVI");
      tvi.Color = Color.Green;
      //Draw( tvi, 4 );

      dsBid = new DoubleSeries("Bid");
      dsBid.Color = Color.Red;
      Draw(dsBid, 0);

      dsAsk = new DoubleSeries("Ask");
      dsAsk.Color = Color.Blue;
      Draw(dsAsk, 0);

      dsBidVolume = new DoubleSeries("Bid Volume");
      dsBidVolume.Color = Color.Red;
      //Draw(dsBidVolume, 2 );

      dsAskVolume = new DoubleSeries("Ask Volume");
      dsAskVolume.Color = Color.Blue;
      //Draw( dsAskVolume, 2 );

      dsLine = new DoubleSeries("Line");
      dsLine.Color = Color.Purple;
      //Draw( dsLine, 0 );

      dsSignal = new DoubleSeries("Signal");
      dsSignal.DrawStyle = EDrawStyle.Circle;
      dsSignal.DrawWidth = 6;
      dsSignal.Color = Color.GreenYellow;
      //Draw( dsSignal, 0 );

      state = PositionState.Empty;

      PatternState = EPatternState.init;
      PatternPt0 = 0;
      PatternPt1 = 0;
      dsPattern = new DoubleSeries("Pattern");
      dsPattern.Color = Color.Chocolate;
      Draw(dsPattern, 0);
      dsPt0Ratio = new DoubleSeries("Pt0 Ratio");
      dsPt0Ratio.Color = Color.Chocolate;
      Draw(dsOne, 8);
      Draw(dsHalf, 8);
      Draw(dsPt0Ratio, 8);
      cntNewUp = 0;
      cntNewDown = 0;
      dsSellDecisionPoint = new DoubleSeries("Sell Decision");
      dsSellDecisionPoint.Color = Color.Red;
      dsSellDecisionPoint.DrawStyle = EDrawStyle.Circle;
      dsSellDecisionPoint.DrawWidth = 7;
      Draw(dsSellDecisionPoint, 0);
      dsBuyDecisionPoint = new DoubleSeries("Buy Decision");
      dsBuyDecisionPoint.Color = Color.Blue;
      dsBuyDecisionPoint.DrawStyle = EDrawStyle.Circle;
      dsBuyDecisionPoint.DrawWidth = 7;
      Draw(dsBuyDecisionPoint, 0);
      bSellDecisionPointSet = false;
      bBuyDecisionPointSet = false;

      szPDD = (int)Math.Round(dblPatternDelta * 100);
      rPatternDeltaDistance = new int[szPDD + 1];
      for (int i = 0; i <= szPDD; i++) rPatternDeltaDistance[i] = 0;

      if (!IsGUIFrozen) bCanUseForms = true;



      rAccum = new AccumulateQuotes[] {
			new AccumulateQuotes( "Accum 096s",  96, 10000, 2, true, Color.DarkGray ),  // #* 1.6 minutes
			new AccumulateQuotes( "Accum 256s", 256, 10000, 2.0, false, Color.Orange ),  // # 4.3 minutes
			new AccumulateQuotes( "Accum 768s", 768, 10000, 2.0, false, Color.DarkOrchid ), // 12.8 minutes
			//new Accumulation( "Accum 4096s", 4096, 10000, 2, bars, BarWidth, true, Color.Lavender ), // 68.3 minutes
			//new Accumulation( "Accum 2048s", 2048, 10000, 2.0, bars, BarWidth, true, true, Color.Fuchsia ), // 34.1 minutes
			//new Accumulation( "Accum 1024s", 1024, 10000, 2, bars, BarWidth, true, Color.DarkSalmon ), // 17 minutes
			//new Accumulation( "Accum 512s", 512, 10000, 1.8, bars, BarWidth, true, Color.Orange ), // 8.5 minutes
			//new Accumulation( "Accum 192s", 192, 10000, 1.8, bars, BarWidth, false, Color.Blue ),  // #*  // 3.2 minutes
			//new Accumulation( "Accum 128s", 128, 10000, 2, bars, BarWidth, true, false, Color.DarkCyan ),  // # 2.1 minutes
			//new Accumulation( "Accum 064s",  64, 10000, 2, bars, BarWidth, true, false, Color.DarkGray ),
			//new Accumulation( "Accum 048s",  48, 10000, 1.8, bars, BarWidth, false, Color.Black ),  //*
			//new Accumulation( "Accum 040s",  40, 10000, 2, bars, BarWidth, true, Color.Black ),
			//new Accumulation( "Accum 032s",  32, 10000, 2, bars, BarWidth, true, Color.Black ),
			//new Accumulation( "Accum 024s",  24, 10000, 2, bars, BarWidth, false, Color.Black ),  //*
			//new Accumulation( "Accum 020s",  20, 10000, 2, bars, BarWidth, true, Color.Black ),
			//new Accumulation( "Accum 016s",  16, 10000, 2, bars, BarWidth, false, Color.Black ),  //*
			//new Accumulation( "Accum 014s",  14, 10000, 2, bars, BarWidth, true, Color.Black ),
			//new Accumulation( "Accum 012s",  12, 10000, 2, bars, BarWidth, false, Color.Black ),  //*
			//new Accumulation( "Accum 010s",  10, 10000, 2, bars, BarWidth, true, Color.Black ),
			//new Accumulation( "Accum 008s",   8, 10000, 2, bars, BarWidth, false, Color.Black ),  //*
			//new Accumulation( "Accum 005s",   5, 10000, 2, bars, BarWidth, false, Color.Orange ),
			//new Accumulation( "Accum 004s",   4, 10000, 2, bars, BarWidth, false, Color.Black ),  //*
			//new Accumulation( "Accum 003s",   3, 10000, 2, bars, BarWidth, true, Color.Black ),
			//new Accumulation( "Accum 002s",   2, 10000, 2, bars, BarWidth, false , Color.Black),  //*
			//new Accumulation( "Accum 001s",   1, 10000, 2, bars, BarWidth, false, Color.Black )   //*
			};
      rAccum[0].EnclosingAccumulation = rAccum[0];
      //Draw( rAccum[0].dsSlope, 4 );
      Draw(rAccum[0].dsSlopeAvg, 4);
      Draw(rAccum[0].dsAccelAvg, 5);
      //Draw( rAccum[0].dsAccel, 5 );
      //Draw( rAccum[0].dsRR, 5 );
      Draw(rAccum[0].dsAvg, 0);
      Draw(rAccum[0].dsBBUpper, 0);
      Draw(rAccum[0].dsBBLower, 0);

      Draw(rAccum[0].dsB, 6);
      Draw(rAccum[0].dsBandwidth, 7);
      //Draw( rAccum[0].dsER, 9 );
      //Draw( rAccum[0].mfi, 10 );
      //if ( rAccum[0].CalcSlope ) Draw( rAccum[0].dsSD, 7 );
      //for ( int i = rAccum.Length - 1; i >= 1; i-- ) {
      for (int i = 1; i <= rAccum.Length - 1; i++) {
        rAccum[i].EnclosingAccumulation = rAccum[i - 1];
        //Draw( rAccum[i].dsSlope, 4 );
        Draw(rAccum[i].dsSlopeAvg, 4);
        Draw(rAccum[i].dsAccelAvg, 5);
        //Draw( rAccum[i].dsAccel, 5 );
        //Draw( rAccum[i].dsRR, 5 );
        Draw(rAccum[i].dsAvg, 0);
        Draw(rAccum[i].dsBBUpper, 0);
        Draw(rAccum[i].dsBBLower, 0);
        Draw(rAccum[i].dsB, 6);
        Draw(rAccum[i].dsBandwidth, 7);
        //Draw( rAccum[i].dsER, 9 );
        //Draw( rAccum[i].mfi, 10 );
        //if ( rAccum[i].CalcSlope ) Draw( rAccum[i].dsSD, 7 );
      }
      Draw(dsOne, 4);
      Draw(dsHalf, 4);
      Draw(dsZero, 4);
      Draw(dsOne, 5);
      Draw(dsHalf, 5);
      Draw(dsZero, 5);
      Draw(dsOne, 6);
      Draw(dsHalf, 6);
      Draw(dsZero, 6);
      Draw(dsOne, 7);
      Draw(dsHalf, 7);
      Draw(dsZero, 7);

      rAccum[0].bbwMax = 3.0;
      rAccum[1].bbwMax = 4.0;
      rAccum[2].bbwMax = 6.5;

      rAccum[0].SlopeAvgScaleMin = -0.01;
      rAccum[0].SlopeAvgScaleMax = 0.01;
      rAccum[1].SlopeAvgScaleMin = -0.005;
      rAccum[1].SlopeAvgScaleMax = 0.005;
      rAccum[2].SlopeAvgScaleMin = -0.0025;
      rAccum[2].SlopeAvgScaleMax = 0.0025;

      rAccum[0].AccelAvgScaleMin = -0.1;
      rAccum[0].AccelAvgScaleMax = 0.1;
      rAccum[1].AccelAvgScaleMin = -0.05;
      rAccum[1].AccelAvgScaleMax = 0.05;
      rAccum[2].AccelAvgScaleMin = -0.01;
      rAccum[2].AccelAvgScaleMax = 0.01;


      //Draw( dsZero, 7 );

      latestRoundTrip = null;
      latestQuote = null;

      //gpstrategy.EmitSignalInit(this);

      DateTime dtStartingDate;
      DateTime dtEndingDate;
      IExecutionProvider ieprov = strat.ExecutionProvider;
      IMarketDataProvider imdprov = strat.MarketDataProvider;
      if (MetaStrategyMode.Live == strat.ATSMetaStrategy.MetaStrategyMode) {
        dtStartingDate = DateTime.Today;
        //string prov = "IQFeed";
        IBarFactory factory = ProviderManager.MarketDataProviders[imdprov.Id].BarFactory;
        factory.Items.Clear();
        factory.Items.Add(new BarFactoryItem(BarType.Time, BarWidth, true));

        instrument.RequestMarketData(
          ProviderManager.MarketDataProviders[imdprov.Id], MarketDataType.Quote);
        instrument.RequestMarketData(
          ProviderManager.MarketDataProviders[imdprov.Id], MarketDataType.Trade);

        //gpstrategy.TradingTimeBegin = new TimeSpan(0, 0, 0);
        //gpstrategy.TradingTimeEnd   = new TimeSpan(23, 59, 0);
        gpstrategy.TradingTimeBegin = new TimeSpan(10, 30, 0);
        gpstrategy.TradingTimeEnd = new TimeSpan(16, 55, 0);
      }
      else {
        dtStartingDate = strat.MetaStrategyBase.SimulationManager.EntryDate;
        dtEndingDate = strat.MetaStrategyBase.SimulationManager.ExitDate;
        Console.WriteLine("{0} Simulation Range {1} to {2}", Instrument.Symbol, dtStartingDate, dtEndingDate);
        IBarFactory factory = ProviderManager.MarketDataProviders[1].BarFactory;
        factory.Items.Clear();
        factory.Items.Add(new BarFactoryItem(BarType.Time, BarWidth, true));

        gpstrategy.TradingTimeBegin = dtStartingDate.TimeOfDay;
        gpstrategy.TradingTimeEnd = dtEndingDate.TimeOfDay - new TimeSpan(0, 3, 0);

        // these lines are located in SimulationManager Component
        //SendMarketDataRequest("Quote");
        //SendMarketDataRequest("Trade");

      }

      //Console.WriteLine( "StartingDate {0}", dtStartingDate );
      dtCurrentDay = dtStartingDate.Date;

      /*
      Bar barDay1;

      int i = daily.GetIndex(dtStartingDate.Date, EIndexOption.Prev);
      barDay1 = daily[i];
      if (barDay1.DateTime.Date == dtStartingDate.Date) {
        i--; // realtime will come up with different one from simulation, depending upon what is dateset 
        // ie, there is no bar or information for today, only the previous trading day
        barDay1 = daily[i];
      }
      */

      //
      // Make sure these are removed in OnStrategyStop
      //

      //GPStrategy.SignalInit += OnSignalInit;
      //GPStrategy.SignalBar += OnSignalBar;
      //GPStrategy.SignalTrade += OnSignalTrade;
      //GPStrategy.SignalQuote += OnSignalQuote;
      //GPStrategy.SignalStage1 += OnSignalStage1;
      //GPStrategy.SignalStage2 += OnSignalStage2;


    }

    void OnSignalInit( object source, GPStrategy gpis ) {
    }

    void OnSignalBar( object source, GPStrategy gpis ) {
    }

    void OnSignalTrade( object source, GPStrategy gpis ) {
    }

    void OnSignalQuote( object source, GPStrategy gpis ) {
    }

    void OnSignalStage1( object source, GPStrategy gpis ) {
    }

    void OnSignalStage2( object source, GPStrategy gpis ) {

      //int sdx = 3;
      try {

        if (StopSignalled || !CanTrade) {
          gpstrategy.ExitSignal = true;
          StopSignalled = false;
        }
        /*
        gpis.BuySignal = ((gpis.dsAsk.Last > gpis.dsBid.Last)
          || (gpis.smaBidAskMidPoint.Ago(intBidAskAgo) < gpis.dsBid.Last));
        gpis.SellSignal = ((gpis.dsAsk.Last > gpis.dsBid.Last)
          || (gpis.dsBid.Last < gpis.dsBidAskKama0.Ago(intKama0Ago)));
    */
        //gpis.BuySignal=gpis.dsBidAskKama0.Last>gpis.dsBidAskKama1.Last;
        //gpis.SellSignal=gpis.dsBidAskKama0.Last<gpis.dsBidAskKama1.Last;

        if (gpstrategy.ExitSignal || (gpstrategy.BuySignal ^ gpstrategy.SellSignal)) {
          //Console.WriteLine("gps state {0} {1} {2} {3}", 
          //	state, gpstrategy.ExitSignal, gpstrategy.BuySignal, gpstrategy.SellSignal);
          switch (state) {
            case PositionState.Empty:
              if (!gpstrategy.ExitSignal) {
                if (gpstrategy.BuySignal) {
                  state = PositionState.LongContinue;
                  trans = new TransactionSet(TransactionSet.ESignal.Long, this, intOrderSize,
                    intScaleInSize, intMaxScaleIn, latestQuote, PredictedPriceJump,
                    hardstop, trailingstop, eventholder);
                  //OnNewMktLongEntry(Instrument, intOrderSize, "Long Entry");
                }
                if (gpstrategy.SellSignal) {
                  state = PositionState.ShortContinue;
                  trans = new TransactionSet(TransactionSet.ESignal.Short, this, intOrderSize,
                    intScaleInSize, intMaxScaleIn, latestQuote, PredictedPriceJump,
                    hardstop, trailingstop, eventholder);
                  //OnNewMktShortEntry(Instrument, intOrderSize, "Short Entry");
                }
              }
              break;
            case PositionState.LongContinue:
              if (gpstrategy.ExitSignal) {
                state = PositionState.Empty;
                trans.UpdateSignal(TransactionSet.ESignal.Exit);
                trans = null;
                //OnNewMktLongExit(Instrument, Math.Abs(PositionRequested), "Long Stop");
              }
              else {
                if (gpstrategy.SellSignal) {
                  state = PositionState.ShortReversed;
                  trans.UpdateSignal(TransactionSet.ESignal.Exit);
                  //OnNewMktLongExit(Instrument, Math.Abs(PositionRequested), "Long Exit");
                  trans = new TransactionSet(TransactionSet.ESignal.Short, this, intOrderSize,
                    intScaleInSize, intMaxScaleIn, latestQuote, PredictedPriceJump,
                    hardstop, trailingstop, eventholder);
                  //OnNewMktShortEntry(Instrument, intOrderSize, "Short Reversal");
                }
                else {
                  if (PositionRequested < intMaxScaleIn) {
                    trans.UpdateSignal(TransactionSet.ESignal.ScaleIn);
                    //OnNewMktLongEntry(Instrument, intOrderSize, "Long Pyramid" );
                  }
                }
              }
              break;
            case PositionState.LongReversed:
              if (gpstrategy.ExitSignal) {
                state = PositionState.Empty;
                trans.UpdateSignal(TransactionSet.ESignal.Exit);
                trans = null;
                //OnNewMktLongExit(Instrument, Math.Abs(PositionRequested), "Long Stop");
              }
              else {
                if (gpstrategy.SellSignal) {
                  state = PositionState.ShortReversed;
                  trans.UpdateSignal(TransactionSet.ESignal.Exit);
                  //OnNewMktLongExit(Instrument, Math.Abs(PositionRequested), "Long Exit");
                  trans = new TransactionSet(TransactionSet.ESignal.Short, this, intOrderSize,
                    intScaleInSize, intMaxScaleIn, latestQuote, PredictedPriceJump,
                    hardstop, trailingstop, eventholder);
                  //OnNewMktShortEntry(Instrument, intOrderSize, "Short Reversal");
                  //OnNewLmtShortEntry(
                  //	Instrument, 
                  //	intOrderSize, Math.Max( signalledQuote.Ask, signalledQuote.Bid ), 
                  //	"Short Reversal");
                }
                else {
                  if (PositionRequested < intMaxScaleIn) {
                    state = PositionState.LongContinue;
                    trans.UpdateSignal(TransactionSet.ESignal.ScaleIn);
                    //OnNewMktLongEntry(Instrument, intOrderSize, "Long Pyramid" );
                  }
                }
              }
              break;
            case PositionState.ShortContinue:
              if (gpstrategy.ExitSignal) {
                state = PositionState.Empty;
                trans.UpdateSignal(TransactionSet.ESignal.Exit);
                trans = null;
                //OnNewMktShortExit(Instrument, Math.Abs(PositionRequested), "Short Stop");
              }
              else {
                if (gpstrategy.BuySignal) {
                  state = PositionState.LongReversed;
                  trans.UpdateSignal(TransactionSet.ESignal.Exit);
                  //OnNewMktShortExit(Instrument, Math.Abs(PositionRequested), "Short Exit");
                  trans = new TransactionSet(TransactionSet.ESignal.Long, this, intOrderSize,
                    intScaleInSize, intMaxScaleIn, latestQuote, PredictedPriceJump,
                    hardstop, trailingstop, eventholder);
                  //OnNewMktLongEntry(Instrument, intOrderSize, "Long Reversal");
                }
                else {
                  if (Math.Abs(PositionRequested) < intMaxScaleIn) {
                    trans.UpdateSignal(TransactionSet.ESignal.ScaleIn);
                    //OnNewMktShortEntry(Instrument, intOrderSize, "Short Pyramid" );
                  }
                }
              }
              break;
            case PositionState.ShortReversed:
              if (gpstrategy.ExitSignal) {
                state = PositionState.Empty;
                trans.UpdateSignal(TransactionSet.ESignal.Exit);
                trans = null;
                //OnNewMktShortExit(Instrument, Math.Abs(PositionRequested), "Short Stop");
              }
              else {
                if (gpstrategy.BuySignal) {
                  state = PositionState.LongReversed;
                  trans.UpdateSignal(TransactionSet.ESignal.Exit);
                  //OnNewMktShortExit(Instrument, Math.Abs(PositionRequested), "Short Exit");
                  trans = new TransactionSet(TransactionSet.ESignal.Long, this, intOrderSize,
                    intScaleInSize, intMaxScaleIn, latestQuote, PredictedPriceJump,
                    hardstop, trailingstop, eventholder);
                  //OnNewMktLongEntry(Instrument, intOrderSize, "Long Reversal");
                  //OnNewLmtLongEntry(
                  //	Instrument, 
                  //	intOrderSize, Math.Min( signalledQuote.Ask, signalledQuote.Bid ), 
                  //	"Long Reversal");
                }
                else {
                  if (Math.Abs(PositionRequested) < intMaxScaleIn) {
                    state = PositionState.ShortContinue;
                    trans.UpdateSignal(TransactionSet.ESignal.ScaleIn);
                    //OnNewMktShortEntry(Instrument, intOrderSize, "Short Pyramid" );
                  }
                }
              }
              break;
          }
          //Console.WriteLine( "gps state exit" );
        }


      }
      catch (Exception e) {
        Console.WriteLine("problems {0}" + e);
      }
    }

    public override void OnBar( Bar bar ) {
      //gpstrategy.EmitSignalBar(this, bar);

      if (bar.DateTime.TimeOfDay <= gpstrategy.TradingTimeEnd
      && bar.DateTime.TimeOfDay >= gpstrategy.TradingTimeBegin) {

        bars.Add(bar);
        if (1 == bars.Count) {
          open = bar.Open;
          dsOpen.Name = "open " + open.ToString("#.00");
        }

        if (bar.High > bar.Low) {
          double ii = bar.Volume * (bar.Close + bar.Close - bar.High - bar.Low) / (bar.High - bar.Low);
          double ad = bar.Volume * (bar.Close - bar.Open) / (bar.High - bar.Low);
          if (dsII.Count > 0 && dsAD.Count > 0) {
            dsII.Add(bar.DateTime, dsII.Last + ii);
            dsAD.Add(bar.DateTime, dsAD.Last + ad);
          }
          else {
            dsII.Add(bar.DateTime, ii);
            dsAD.Add(bar.DateTime, ad);
          }
        }

        dsOpen.Add(bar.DateTime, open);

        dsDayCl.Add(bar.DateTime, daycl);
        dsDayPv.Add(bar.DateTime, dayPv);
        dsDayR1.Add(bar.DateTime, dayR1);
        dsDayR2.Add(bar.DateTime, dayR2);
        dsDayR3.Add(bar.DateTime, dayR3);
        dsDayS1.Add(bar.DateTime, dayS1);
        dsDayS2.Add(bar.DateTime, dayS2);
        dsDayS3.Add(bar.DateTime, dayS3);

        dsWeekPv.Add(bar.DateTime, weekPv);
        dsWeekR1.Add(bar.DateTime, weekR1);
        dsWeekR2.Add(bar.DateTime, weekR2);
        dsWeekR3.Add(bar.DateTime, weekR3);
        dsWeekS1.Add(bar.DateTime, weekS1);
        dsWeekS2.Add(bar.DateTime, weekS2);
        dsWeekS3.Add(bar.DateTime, weekS3);

        dsMonPv.Add(bar.DateTime, monPv);
        dsMonR1.Add(bar.DateTime, monR1);
        dsMonR2.Add(bar.DateTime, monR2);
        dsMonR3.Add(bar.DateTime, monR3);
        dsMonS1.Add(bar.DateTime, monS1);
        dsMonS2.Add(bar.DateTime, monS2);
        dsMonS3.Add(bar.DateTime, monS3);

        dsSma20Day.Add(bar.DateTime, sma20day);
        dsSma200Day.Add(bar.DateTime, sma200day);

        dsZero.Add(bar.DateTime, 0.0);
        dsHalf.Add(bar.DateTime, 0.5);
        dsOne.Add(bar.DateTime, 1.0);

      }
    }

    public override void OnTrade( Trade trade ) {

      //string s = trade.DateTime.ToString("HH:mm:ss.fff");
      //Console.WriteLine( "onTrade {0} {1:#.00} {2:#.00}", s, trade.Price, trade.Size );

      //gpstrategy.EmitSignalStage1(this);  // this clears signals so needs to come first

      CanTrade = true;

      if ((trade.DateTime.TimeOfDay > gpstrategy.TradingTimeEnd)
      || (trade.DateTime.TimeOfDay < gpstrategy.TradingTimeBegin)) {
        //Console.WriteLine("{0} {1} {2} {3}", 
        //	quote.DateTime.TimeOfDay, gpstrategy.TradingTimeEnd,
        //	quote.DateTime.TimeOfDay, gpstrategy.TradingTimeBegin );
        //	gpstrategy.ExitSignal = true;
      }

      cntTrades++;

      dsTrade.Add(trade.DateTime, trade.Price);
      dsTradeVolume.Add(trade.DateTime, trade.Size);

      //tvi.Add( trade );
      //if ( null != latestQuote ) tvi.Add( trade, latestQuote );

      if (slVolumePerPrice.ContainsKey(trade.Price)) {
        int vol = (int)slVolumePerPrice[trade.Price];
        vol += trade.Size;
        slVolumePerPrice[trade.Price] = vol;
      }
      else {
        slVolumePerPrice.Add(trade.Price, trade.Size);
      }

      TimeSpan ts = new TimeSpan(trade.DateTime.Hour, trade.DateTime.Minute, trade.DateTime.Second);
      if (ts != tsTradeSpan) {
        if (0 != TradesPerSecondCount) {
          dsTradesPerSecond.Add(trade.DateTime, TradesPerSecondCount);
          dsVolumePerSecond.Add(trade.DateTime, TradeVolumePerSecond);
          if (TradeHigh != TradeLow) {
            dsVolumePerRange.Add(trade.DateTime, TradeVolumePerSecond / (TradeHigh - TradeLow));
          }
          TradesPerSecondCount = 0;
          TradeVolumePerSecond = 0;
          TradeHigh = 0;
          TradeLow = 0;
        }
        tsTradeSpan = ts;
      }
      else {
        if (0 == TradesPerSecondCount) {
          TradeHigh = trade.Price;
          TradeLow = trade.Price;
        }
        else {
          TradeHigh = Math.Max(TradeHigh, trade.Price);
          TradeLow = Math.Min(TradeLow, trade.Price);
        }
        TradeVolumePerSecond += trade.Size;
        TradesPerSecondCount++;
      }


      //if ( null != LastQuote ) gpstrategy.EmitSignalQuote(this, LastQuote);
      //gpstrategy.EmitSignalTrade(this, trade);

      //dsZero.Add(trade.DateTime, 0);

      //gpstrategy.EmitSignalStage2(this);
    }

    private void OrderBookQuoteEvent( object o, EventArgs args ) {
      Quote quote = null;
      lock (ob) {
        if (ob.slAsk.Count > 0 && ob.slBid.Count > 0) {
          MarketMakerBidAsk mmbaBid = (MarketMakerBidAsk)ob.slBid.GetByIndex(0);
          MarketMakerBidAsk mmbaAsk = (MarketMakerBidAsk)ob.slAsk.GetByIndex(0);
          quote = new Quote(Clock.Now, mmbaBid.Bid, mmbaBid.BidSize, mmbaAsk.Ask, mmbaAsk.AskSize);
        }
      }

      if (null != quote) {
        //Console.WriteLine( "1 Quote {0} {1} {2}", quote.DateTime, quote.Bid, quote.Ask );
        OnQuote(quote);
      }
    }

    public override void OnQuote( Quote quote ) {

      //string s = quote.DateTime.ToString("HH:mm:ss.fff");
      //Console.WriteLine( "onQuote {0} b/a {1:@#.00}/{2:#.00}", 
      //	s, quote.Bid, quote.Ask );

      //Console.WriteLine( "2 Quote {0} {1} {2}", quote.DateTime, quote.Bid, quote.Ask );

      double qHi;
      double qLo;
      qHi = Math.Max(quote.Ask, quote.Bid);
      qLo = Math.Min(quote.Ask, quote.Bid);

      if (null != latestQuote) {

        double lqHi = Math.Max(latestQuote.Ask, latestQuote.Bid);
        double lqLo = Math.Min(latestQuote.Ask, latestQuote.Bid);

      }
      latestQuote = quote;
      if (null != latestRoundTrip) {
        latestRoundTrip.UpdateQuote(quote);
      }

      eventholder.UpdateQuote(this, quote);

      //qa1.Add(quote);

      //gpstrategy.EmitSignalStage1(this);  // this clears signals so needs to come first

      //gpstrategy.Reset();

      if ((quote.DateTime.TimeOfDay > gpstrategy.TradingTimeEnd)
      || (quote.DateTime.TimeOfDay < gpstrategy.TradingTimeBegin)) {
        //Console.WriteLine("{0} {1} {2} {3}", 
        //	quote.DateTime.TimeOfDay, gpstrategy.TradingTimeEnd,
        //	quote.DateTime.TimeOfDay, gpstrategy.TradingTimeBegin );
        gpstrategy.ExitSignal = true;
      }
      else {

        if (0 == cntQuotes) {
          dtLastDirectionSignal = quote.DateTime;
        }

        cntQuotes++;

        dsBid.Add(quote.DateTime, quote.Bid);
        dsAsk.Add(quote.DateTime, quote.Ask);

        dsBidVolume.Add(quote.DateTime, -quote.BidSize);
        dsAskVolume.Add(quote.DateTime, quote.AskSize);

        TimeSpan ts = new TimeSpan(quote.DateTime.Hour, quote.DateTime.Minute, quote.DateTime.Second);
        if (ts != tsQuoteSpan) {
          if (0 != QuotesPerSecondCount) {
            dsQuotesPerSecond.Add(quote.DateTime, QuotesPerSecondCount);
            QuotesPerSecondCount = 0;
          }
          tsQuoteSpan = ts;
        }
        else {
          QuotesPerSecondCount++;
        }

        double val = (qHi + qLo) / 2.0;
        //double val =( quote.Ask + quote.Bid ) / 2.0;
        double dif;


        //
        // Pattern calculation 
        //
        //bSellDecisionPointSet = false;
        //bBuyDecisionPointSet = false;
        switch (PatternState) {
          case EPatternState.init:
            PatternPt1 = val;
            PatternPt0 = val;
            PatternState = EPatternState.start;
            dsPattern.Add(quote.DateTime, val);
            //Console.WriteLine( "{0} Pattern init {1}", SmartQuant.Clock.Now, PatternState );  
            break;
          case EPatternState.start:
            if (Math.Abs(val - PatternPt1) >= dblPatternDelta) {
              dtPatternPt1 = Clock.Now;
              PatternPt0 = val;
              if (val > PatternPt1) {
                PatternState = EPatternState.up;
                //bBuyDecisionPointSet = true;  // opposite of its regular use in order to initiate trending trade
                gpstrategy.BuySignal = true;
                //Console.WriteLine( "{0} Pattern start {1}", SmartQuant.Clock.Now, PatternState );  
              }
              else {
                PatternState = EPatternState.down;
                //bSellDecisionPointSet = true; // opposite of its regular use in order to initiate trending trade
                gpstrategy.SellSignal = true;
                //Console.WriteLine( "{0} Pattern start {1}", SmartQuant.Clock.Now, PatternState );  
              }
              PatternPt1 = val;
            }
            break;
          case EPatternState.up:
            val = quote.Bid;  // will need to sell on the bid
            PatternPt0 = val;
            if (val > PatternPt1) {
              PatternPt1 = val;
              dtPatternPt1 = Clock.Now;
              cntNewUp++;
              dsSellDecisionPoint.Add(quote.DateTime, val);
              bSellDecisionPointSet = true;
              bDisplayDepth = true;
            }
            dif = PatternPt1 - PatternPt0;
            dsPt0Ratio.Add(quote.DateTime, dif / dblPatternDelta);
            //dif = Math.Abs( dif );
            if (dif >= dblPatternDelta) {
              dsPattern.Add(dtPatternPt1, PatternPt1);
              mp.ClassifyDoubleSeriesEnd(dsPattern);
              if (PatternPt1 > PatternPt0) {
                //Console.WriteLine( "{0} Pattern from {1}", SmartQuant.Clock.Now, PatternState );  
                PatternState = EPatternState.down;
                //gpstrategy.SellSignal = true;
              }
              else {
                //Console.WriteLine( "{0} Pattern already {1}", SmartQuant.Clock.Now, PatternState );  
              }
            }
            else {
              rPatternDeltaDistance[(int)Math.Round(dif * 100)]++;
            }
            break;
          case EPatternState.down:
            val = quote.Ask;  // will need to buy on the ask
            PatternPt0 = val;
            if (val < PatternPt1) {
              PatternPt1 = val;
              dtPatternPt1 = Clock.Now;
              cntNewDown++;
              dsBuyDecisionPoint.Add(quote.DateTime, val);
              bBuyDecisionPointSet = true;
              bDisplayDepth = true;
            }
            dif = PatternPt0 - PatternPt1;
            dsPt0Ratio.Add(quote.DateTime, dif / dblPatternDelta);
            //dif = Math.Abs( dif );
            if (dif >= dblPatternDelta) {
              dsPattern.Add(dtPatternPt1, PatternPt1);
              mp.ClassifyDoubleSeriesEnd(dsPattern);
              if (PatternPt1 < PatternPt0) {
                //Console.WriteLine( "{0} Pattern from {1}", SmartQuant.Clock.Now, PatternState );  
                PatternState = EPatternState.up;
                //gpstrategy.BuySignal = true;
              }
              else {
                //Console.WriteLine( "{0} Pattern already {1}", SmartQuant.Clock.Now, PatternState );  
              }
            }
            else {
              rPatternDeltaDistance[(int)Math.Round(dif * 100)]++;
            }
            break;
        }

        // updates accumulation stats
        ECross cross;
        foreach (AccumulateQuotes accum in rAccum) {
          //accum.Add( quote.DateTime, val, avgQuoteSpread );
          accum.Add(quote);
          if (accum.CalcTrade) {

            if (bSellDecisionPointSet) {
              //gpstrategy.BuySignal = true;
              cross = accum.dsB.Crosses(1.0, accum.dsB.Count - 1);
              if (ECross.Below == cross) {
                gpstrategy.SellSignal = true;
                //gpstrategy.BuySignal = false;
                bSellDecisionPointSet = false;
              }
            }
            if (bBuyDecisionPointSet) {
              //gpstrategy.SellSignal = true;
              cross = accum.dsB.Crosses(0.0, accum.dsB.Count - 1);
              if (ECross.Above == cross) {
                gpstrategy.BuySignal = true;
                //gpstrategy.SellSignal = false;
                bBuyDecisionPointSet = false;
              }
            }

          }

        }




        /*			
        if ( gpstrategy.BuySignal || gpstrategy.SellSignal ) {
          if ( new TimeSpan( 0, 0, 2 ) > ( quote.DateTime.TimeOfDay - dtLastDirectionSignal.TimeOfDay ) ) {
          }
        }
        */

        bool bExit = false;

        if (bExit) {
          //gpstrategy.ExitSignal = true;
          gpstrategy.BuySignal = false;
          gpstrategy.SellSignal = false;
        }

        if (gpstrategy.BuySignal || gpstrategy.SellSignal) {
          dtLastDirectionSignal = quote.DateTime;
        }
        else {
          if (quote.DateTime > (dtLastDirectionSignal + new TimeSpan(0, 0, 0, 0, intExitWait))) {
            gpstrategy.ExitSignal = true;
          }
        }

        //gpstrategy.EmitSignalQuote(this, quote);
        //gpstrategy.EmitSignalStage2(this);

      }

      OnSignalStage2(this, gpstrategy);

    }

    public override void OnMarketDepth( MarketDepth depth ) {

      if (bCanUseForms) {
        if (!bFormsLoaded) {
          l2vu1 = new frmOrderBookView1(Instrument.Symbol);
          l2vu1.Show();
          l2vu1.DesktopLocation = new System.Drawing.Point(400, 175);

          l2vu2 = new frmOrderBookView2(Instrument.Symbol);
          l2vu2.Show();
          l2vu2.DesktopLocation = new System.Drawing.Point(200, 175);

          bFormsLoaded = true;
        }
      }

      lock (ob) {
        ob.Update(depth);
      }

      double maxdistance = 0.60;

      double maxStepPrice;
      int ixStep;
      int maxBidStep = 0;
      int maxAskStep = 0;
      double sumQuanPrice;
      double sumQuan;
      double sumWQuanPrice = 0;  // weighted 
      double sumWQuan = 0;

      //bool bFirstStep;

      int maxBidSteps = 0;
      int maxAskSteps = 0;

      double BidDepth = 0;
      double AskDepth = 0;

      if (bDisplayDepth &&
        (depth.DateTime.TimeOfDay > (LastOrderBookSample + OrderBookSampleInterval) && ob.slAsk.Count > 4 && ob.slBid.Count > 4)
      ) {
        LastOrderBookSample = depth.DateTime.TimeOfDay;

        if (bCanUseForms) {
          l2vu1.RedrawDisplay(ob);
          l2vu2.RedrawDisplay(ob);
        }

        MarketMakerBidAsk mmbaBid = (MarketMakerBidAsk)ob.slBid.GetByIndex(0);
        MarketMakerBidAsk mmbaAsk = (MarketMakerBidAsk)ob.slAsk.GetByIndex(0);

        int ix;

        sumQuanPrice = 0;
        sumQuan = 0;
        maxBidStep = 0;
        maxStepPrice = 0;
        ix = ob.slPrice.IndexOfKey(mmbaBid.Bid);  // start at highest bid and work down 
        while (ix >= 0) {
          double price = (double)ob.slPrice.GetKey(ix);
          int quan = (int)ob.slPrice.GetByIndex(ix);

          if (0 == maxBidStep) {
            maxStepPrice = price - maxdistance;
          }
          else {
            if (price < maxStepPrice) {
              break;
            }
          }
          maxBidStep++;

          if (2 == maxBidStep) {
            //l2Bid[ 0 ].Add( depth.DateTime, price );
          }

          sumQuanPrice += quan * price;
          sumQuan += quan;
          sumWQuanPrice += quan * price;
          sumWQuan += quan;

          ix--;

          if (maxBidStep >= l2maxsteps) {
            break;
          }
        }
        BidDepth = sumQuanPrice / sumQuan;

        sumQuanPrice = 0;
        sumQuan = 0;
        maxAskStep = 0;
        maxStepPrice = 0;
        ix = ob.slPrice.IndexOfKey(mmbaAsk.Ask);  // start at lowest ask and work up 
        while (ix < ob.slPrice.Count) {
          double price = (double)ob.slPrice.GetKey(ix);
          int quan = (int)ob.slPrice.GetByIndex(ix);

          if (0 == maxAskStep) {
            maxStepPrice = price + maxdistance;
          }
          else {
            if (price > maxStepPrice) {
              break;
            }
          }
          maxAskStep++;

          if (2 == maxAskStep) {
            //l2Ask[ 0 ].Add( depth.DateTime, price );
          }

          sumQuanPrice += quan * price;
          sumQuan += quan;
          sumWQuanPrice += quan * price;
          sumWQuan += quan;

          ix++;

          if (maxAskStep >= l2maxsteps) {
            break;
          }
        }
        AskDepth = sumQuanPrice / sumQuan;


        double pressure = sumWQuanPrice / sumWQuan;
        dsBidDepth.Add(depth.DateTime, BidDepth);
        dsAskDepth.Add(depth.DateTime, AskDepth);
        dsDepth.Add(depth.DateTime, pressure);

        if (null != latestQuote) {
          double mid = (latestQuote.Ask + latestQuote.Bid) / 2;
          dsPressureBar.Add(depth.DateTime, pressure - mid);
          double wide = AskDepth - BidDepth;
          dsPressureBarNormalized.Add(depth.DateTime, (pressure - BidDepth - wide / 2) / (wide));
        }
      }


    }

    public override void OnStrategyStop() {
      //Console.WriteLine( "OnStrategyStop");
      //GPStrategy.SignalQuote -= OnSignalQuote;
      //GPStrategy.SignalStage1 -= OnSignalStage1;
      //GPStrategy.SignalStage2 -= OnSignalStage2;

      //Console.WriteLine( "#Quotes {0}", cntQuotes );
      eventholder.StrategyStop(this, EventArgs.Empty);
      eventholder.UpdateSignalStatus -= OnTransactionSetExit;

      //ob.InsideQuoteChangedEventHandler -= new EventHandler(OrderBookQuoteEvent);


      //l2vu1.Close();
      //l2vu2.Close();

      double SumY = 0;
      double SumYY = 0;
      int Xcnt = 0;
      for (int i = 1; i < dsPattern.Count; i++) {
        double val = Math.Abs(dsPattern[i] - dsPattern[i - 1]);
        SumY += val;
        SumYY += val * val;
        Xcnt++;
      }

      double Syy = SumYY - SumY * SumY / Xcnt;

      Console.WriteLine("{0} Pattern Segments Avg {1:#.00} SD {2:#.00}, Decision Points: up {3} down {4} dif {5}",
        Xcnt, SumY / Xcnt, Math.Sqrt(Syy / Xcnt), cntNewUp, cntNewDown, cntNewUp - cntNewDown);
      if (dsPt0Ratio.Count > 0) {
        double mean = dsPt0Ratio.GetMean();
        double sd = dsPt0Ratio.GetStdDev();
        Console.WriteLine("dsPt0Ratio avg {0:0.00} + sd {1:0.00} = {2:0.00}", mean, sd, mean + sd);
      }

      Console.WriteLine("Time Stat:  Start {0} Duration {1}", dtRunningStart, DateTime.Now - dtRunningStart);

      /*
      for ( int i = 0; i < rAccum.Length; i++ ) {
        double mean = rAccum[i].slopeAvg.dsSlope.GetMean();
        double sd = rAccum[i].slopeAvg.dsSlope.GetStdDev();
        Console.WriteLine( "dsSlopeAvg {0} mean {1:0.00000} sd {2:0.00000} min {3:0.00000} max {4:0.00000} lo {5:0.0000} hi {6:0.0000}",
          rAccum[i].slopeAvg.dsSlope.Name, 
          mean, 
          sd, 
          rAccum[i].slopeAvg.dsSlope.GetMin(),
          rAccum[i].slopeAvg.dsSlope.GetMax(),
          mean - 2 * sd,
          mean + 2 * sd
          );
      }
      */

      /*
      for ( int i = 0; i < rAccum.Length; i++ ) {
        double mean = rAccum[i].dsBandwidth.GetMean();
        double sd = rAccum[i].dsBandwidth.GetStdDev();
        Console.WriteLine( "dsBandwidth {0} mean {1:0.00000} sd {2:0.00000} min {3:0.00000} max {4:0.00000} lo {5:0.0000} hi {6:0.0000}",
          rAccum[i].dsBandwidth.Name, 
          mean, 
          sd, 
          rAccum[i].dsBandwidth.GetMin(),
          rAccum[i].dsBandwidth.GetMax(),
          mean - 2 * sd,
          mean + 2 * sd
          );
      }
      */

      /*
      for ( int i = 0; i < rAccum.Length; i++ ) {
        double mean = rAccum[i].dsAccelAvg.GetMean();
        double sd = rAccum[i].dsAccelAvg.GetStdDev();
        Console.WriteLine( "dsAccelAvg {0} mean {1:0.00000} sd {2:0.00000} min {3:0.00000} max {4:0.00000} lo {5:0.0000} hi {6:0.0000}",
          rAccum[i].dsAccelAvg.Name, 
          mean, 
          sd, 
          rAccum[i].dsAccelAvg.GetMin(),
          rAccum[i].dsAccelAvg.GetMax(),
          mean - 2 * sd,
          mean + 2 * sd
          );
      }
      */


      /*
      double sumPat = 0;
      double accumPat = 0;
      for ( int i = 0; i <= szPDD; i++ ) {
        sumPat += rPatternDeltaDistance[i];
      }
      for ( int i = 0; i <= szPDD; i++ ) {
        double ratio = 100.0 * rPatternDeltaDistance[i] / sumPat;
        accumPat += ratio;
        Console.WriteLine( "Pattern[{0}] is {1}, {2:#.00}-{3:#.00}", i, rPatternDeltaDistance[i], ratio, accumPat );
      }
      */

      /*
      try {
        foreach (MarketMakerBidAsk mmba in  ob.htMMInfo.Values ) {
          Console.WriteLine( "mm {0} = b {1}/{2}, a {3}/{4}", 
            mmba.MMID, 
            mmba.BidActivity, mmba.BidInside, 
            mmba.AskActivity, mmba.AskInside );
        }
      }
      catch (Exception e ) {
        Console.WriteLine( "Exception e={0}", e );
      }
      */
    }

    private void OnTransactionSetExit( object source, bool Exited ) {
      if (Exited) {
        TransactionSet trans = (TransactionSet)source;
        if (this.trans == trans) {
          //Console.WriteLine( "onTransactionSetExit" );
          state = PositionState.Empty;
        }
      }
    }


  }
}