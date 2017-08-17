using System;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

using System.Data;
using System.Reflection;
using System.Data.SqlClient;

using OneUnified.SmartQuant;
using OneUnified.IQFeed;
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

public class Marker {

	string Legend;
	public DoubleSeries ds;
	public EPivotType PivotType;
	public EPivotDuration PivotDuration;
	public int Level = 0;  // 1, 2, 3, eg R1, S2
	public int Count = 1;
	public double Val;
	Color color = Color.Black;
	
	public enum EPivotType { Open, Xvr, Mean, Pivot, MidPoint, Resistance, Support, Other };
	public enum EPivotDuration { Day, Day3, Week, Month, Other };

	public Marker( 
		string Legend, 
		double Val,
		EPivotType PivotType,
		EPivotDuration PivotDuration, 
		int Level,
		Color color
		) {
		this.Legend = Legend;
		this.PivotType = PivotType;
		this.PivotDuration = PivotDuration;
		this.Level = Level;
		//this.Val = Math.Round( Val, 2 );
		this.Val = Val;
		this.color = color;
		
		Init();
	}
	
	private void Init() {
		ds = new DoubleSeries( Val.ToString( "#.00 " ) + Legend );
		ds.Color = color;
	}
	 
}

public class Pivots {
	
	private double open;
	private double dayhi;
	private double daylo;
	private double daycl;
	
	private double day3hi;
	private double day3lo;
		
	private double weekhi;
	private double weeklo;
	private double weekcl;
		
	private double monhi;
	private double monlo;
	private double moncl;
	
	private string Symbol;
	
	private double dayR3;
	private double dayR2;
	private double dayR1;
	private double dayPv;
	private double dayS1;
	private double dayS2;
	private double dayS3;
	
	private double day3R3;
	private double day3R2;
	private double day3R1;
	private double day3Pv;
	private double day3S1;
	private double day3S2;
	private double day3S3;
	
	private double weekR3;
	private double weekR2;
	private double weekR1;
	private double weekPv;
	private double weekS1;
	private double weekS2;
	private double weekS3;
	
	private double monR3;
	private double monR2;
	private double monR1;
	private double monPv;
	private double monS1;
	private double monS2;
	private double monS3;
	
	private double sma20day;
	private double sma200day;
	
	private double sixmonposmean;
	private double sixmonnegmean;
	private double sixmonpossd;
	private double sixmonnegsd;
	
	public double sixmonposcrossover;
	public double sixmonnegcrossover;
	
	public double AverageDelta;
	
	private SortedList slMarkers;  // keyed with double value, holds structMarker
	//private Hashtable htMarkers;  // keyed with name, returns double for indexing into slMarkers
	
	public Pivots( String Symbol ) {
		
		this.Symbol = Symbol;
		
		TradeDB db1 = new TradeDB();
		db1.Open();
		
		string sql = "select dayhi, daylo, daycl, day3hi, day3lo, weekhi, weeklo, weekcl, monhi, monlo, moncl, sma20day, sma200day"
			+ ", sixmonposmean, sixmonnegmean, sixmonpossd, sixmonnegsd"
			+ " from totrade"
			//+ " where tradesystem='darvas'"
			//+ " and symbol = '" + Symbol + "'"; 
			+ " where symbol = '" + Symbol + "'"; 
		SqlCommand cmd = new SqlCommand( sql, db1.Connection );
		
		SqlDataReader dr;
		dr = cmd.ExecuteReader();
		if ( dr.HasRows ) {
			while ( dr.Read() ) {
				dayhi = dr.GetDouble(0);
				daylo = dr.GetDouble(1);
				daycl = dr.GetDouble(2);
				day3hi = dr.GetDouble(3);
				day3lo = dr.GetDouble(4);
				weekhi = dr.GetDouble(5);
				weeklo = dr.GetDouble(6);
				weekcl = dr.GetDouble(7);
				monhi = dr.GetDouble(8);
				monlo = dr.GetDouble(9);
				moncl = dr.GetDouble(10);
				sma20day = dr.GetDouble(11);
				sma200day = dr.GetDouble(12);
				sixmonposmean = dr.GetDouble(13);
				sixmonnegmean = dr.GetDouble(14);
				sixmonpossd = dr.GetDouble(15);
				sixmonnegsd = dr.GetDouble(16);
			}
		}
		
		dr.Close();
		db1.Close();
		
		sixmonposcrossover = sixmonposmean - sixmonpossd;
		sixmonnegcrossover = sixmonnegmean + sixmonnegsd;

		dayPv = ( dayhi + daylo + daycl ) / 3;
		dayR1 = 2 * dayPv - daylo;
		dayR2 = dayPv + ( dayhi - daylo );
		dayR3 = dayR1 + ( dayhi - daylo );
		dayS1 = 2 * dayPv - dayhi;
		dayS2 = dayPv - ( dayhi - daylo );
		dayS3 = dayS1 - ( dayhi - daylo );
		
		day3Pv = ( day3hi + day3lo + daycl ) / 3;
		day3R1 = 2 * day3Pv - day3lo;
		day3R2 = day3Pv + ( day3hi - day3lo );
		day3R3 = day3R1 + ( day3hi - day3lo );
		day3S1 = 2 * day3Pv - day3hi;
		day3S2 = day3Pv - ( day3hi - day3lo );
		day3S3 = day3S1 - ( day3hi - day3lo );
		
		weekPv = ( weekhi + weeklo + weekcl ) / 3;
		weekR1 = 2 * weekPv - weeklo;
		weekR2 = weekPv + ( weekhi - weeklo );
		weekR3 = weekR1 + ( weekhi - weeklo );
		weekS1 = 2 * weekPv - weekhi;
		weekS2 = weekPv - ( weekhi - weeklo );
		weekS3 = weekS1 - ( weekhi - weeklo );
		
		monPv = ( monhi + monlo + moncl ) / 3;
		monR1 = 2 * monPv - monlo;
		monR2 = monPv + ( monhi - monlo );
		monR3 = monR1 + ( monhi - monlo );
		monS1 = 2 * monPv - monhi;
		monS2 = monPv - ( monhi - monlo );
		monS3 = monS1 - ( monhi - monlo );
		
		slMarkers = new SortedList( 50 );
		
		AddMarker( new Marker( "sma 20", sma20day, Marker.EPivotType.Other, Marker.EPivotDuration.Other, 0, Color.DarkSlateBlue ) );
		AddMarker( new Marker( "sma 200", sma200day, Marker.EPivotType.Other, Marker.EPivotDuration.Other, 0, Color.DarkSeaGreen ) );
		
		AddMarker( new Marker( "CLOSE", daycl, Marker.EPivotType.Other, Marker.EPivotDuration.Day, 0, Color.Black ) );
		
		AddMarker( new Marker( "day R3",    dayR3,            Marker.EPivotType.Resistance, Marker.EPivotDuration.Day, 3, Color.Orange ) );
		AddMarker( new Marker( "day MR23", (dayR3 + dayR2)/2, Marker.EPivotType.Resistance, Marker.EPivotDuration.Day, 0, Color.Purple ) );
		AddMarker( new Marker( "day R2",    dayR2,            Marker.EPivotType.Resistance, Marker.EPivotDuration.Day, 2, Color.Red ) );
		AddMarker( new Marker( "day MR12", (dayR2 + dayR1)/2, Marker.EPivotType.Resistance, Marker.EPivotDuration.Day, 0, Color.Purple ) );
		AddMarker( new Marker( "day R1",    dayR1,            Marker.EPivotType.Resistance, Marker.EPivotDuration.Day, 1, Color.Blue ) );
		AddMarker( new Marker( "day MRP1", (dayPv + dayR1)/2, Marker.EPivotType.Resistance, Marker.EPivotDuration.Day, 0, Color.Purple ) );
		AddMarker( new Marker( "day pv",    dayPv,            Marker.EPivotType.Pivot,      Marker.EPivotDuration.Day, 0, Color.Green ) );
		AddMarker( new Marker( "day MSP1", (dayPv + dayS1)/2, Marker.EPivotType.Support,    Marker.EPivotDuration.Day, 0, Color.Purple ) );
		AddMarker( new Marker( "day S1",    dayS1,            Marker.EPivotType.Support,    Marker.EPivotDuration.Day, 1, Color.Blue ) );
		AddMarker( new Marker( "day MS12", (dayS1 + dayS2)/2, Marker.EPivotType.Support,    Marker.EPivotDuration.Day, 0, Color.Purple ) );
		AddMarker( new Marker( "day S2",    dayS2,            Marker.EPivotType.Support,    Marker.EPivotDuration.Day, 2, Color.Red ) );
		AddMarker( new Marker( "day MS23", (dayS2 + dayS3)/2, Marker.EPivotType.Support,    Marker.EPivotDuration.Day, 0, Color.Purple ) );
		AddMarker( new Marker( "day S3",    dayS3,            Marker.EPivotType.Support,    Marker.EPivotDuration.Day, 3, Color.Orange ) );
			
		AddMarker( new Marker( "day3 R3",    day3R3,             Marker.EPivotType.Resistance, Marker.EPivotDuration.Day3, 3, Color.Orange ) );
		//AddMarker( new Marker( "day3 MR23", (day3R3 + day3R2)/2, Marker.EPivotType.Resistance, Marker.EPivotDuration.Day3, 0, Color.Purple ) );
		AddMarker( new Marker( "day3 R2",    day3R2,             Marker.EPivotType.Resistance, Marker.EPivotDuration.Day3, 2, Color.Red ) );
		//AddMarker( new Marker( "day3 MR12", (day3R2 + day3R1)/2, Marker.EPivotType.Resistance, Marker.EPivotDuration.Day3, 0, Color.Purple ) );
		AddMarker( new Marker( "day3 R1",    day3R1,             Marker.EPivotType.Resistance, Marker.EPivotDuration.Day3, 1, Color.Blue ) );
		//AddMarker( new Marker( "day3 MRP1", (day3Pv + day3R1)/2, Marker.EPivotType.Resistance, Marker.EPivotDuration.Day3, 0, Color.Purple ) );
		AddMarker( new Marker( "day3 pv",    day3Pv,             Marker.EPivotType.Pivot,      Marker.EPivotDuration.Day3, 0, Color.Green ) );
		//AddMarker( new Marker( "day3 MSP1", (day3Pv + day3S1)/2, Marker.EPivotType.Support,    Marker.EPivotDuration.Day3, 0, Color.Purple ) );
		AddMarker( new Marker( "day3 S1",    day3S1,             Marker.EPivotType.Support,    Marker.EPivotDuration.Day3, 1, Color.Blue ) );
		//AddMarker( new Marker( "day3 MS12", (day3S1 + day3S2)/2, Marker.EPivotType.Support,    Marker.EPivotDuration.Day3, 0, Color.Purple ) );
		AddMarker( new Marker( "day3 S2",    day3S2,             Marker.EPivotType.Support,    Marker.EPivotDuration.Day3, 2, Color.Red ) );
		//AddMarker( new Marker( "day3 MS23", (day3S2 + day3S3)/2, Marker.EPivotType.Support,    Marker.EPivotDuration.Day3, 0, Color.Purple ) );
		AddMarker( new Marker( "day3 S3",    day3S3,             Marker.EPivotType.Support,    Marker.EPivotDuration.Day3, 3, Color.Orange ) );
			
		AddMarker( new Marker( "week R3", weekR3, Marker.EPivotType.Resistance, Marker.EPivotDuration.Week, 3, Color.Orange ) );
		AddMarker( new Marker( "week R2", weekR2, Marker.EPivotType.Resistance, Marker.EPivotDuration.Week, 2, Color.Red ) );
		AddMarker( new Marker( "week R1", weekR1, Marker.EPivotType.Resistance, Marker.EPivotDuration.Week, 1, Color.Blue ) );
		AddMarker( new Marker( "week pv", weekPv, Marker.EPivotType.Pivot, Marker.EPivotDuration.Week, 0, Color.Green ) );
		AddMarker( new Marker( "week S1", weekS1, Marker.EPivotType.Support, Marker.EPivotDuration.Week, 1,Color. Blue ) );
		AddMarker( new Marker( "week S2", weekS2, Marker.EPivotType.Support, Marker.EPivotDuration.Week, 2, Color.Red ) );
		AddMarker( new Marker( "week S3", weekS3, Marker.EPivotType.Support, Marker.EPivotDuration.Week, 3, Color.Orange ) );
			
		AddMarker( new Marker( "mon R3", monR3, Marker.EPivotType.Resistance, Marker.EPivotDuration.Month, 3, Color.Orange ) );
		AddMarker( new Marker( "mon R2", monR2, Marker.EPivotType.Resistance, Marker.EPivotDuration.Month, 2, Color.Red ) );
		AddMarker( new Marker( "mon R1", monR1, Marker.EPivotType.Resistance, Marker.EPivotDuration.Month, 1, Color.Blue ) );
		AddMarker( new Marker( "mon pv", monPv, Marker.EPivotType.Pivot, Marker.EPivotDuration.Month, 0, Color.Green ) );
		AddMarker( new Marker( "mon S1", monS1, Marker.EPivotType.Support, Marker.EPivotDuration.Month, 1, Color.Blue ) );
		AddMarker( new Marker( "mon S2", monS2, Marker.EPivotType.Support, Marker.EPivotDuration.Month, 2, Color.Red ) );
		AddMarker( new Marker( "mon S3", monS3, Marker.EPivotType.Support, Marker.EPivotDuration.Month, 3, Color.Orange ) );
			
	}
	
	public double GetKey( int ix ) {
		double val = 0;
		if ( ix >= 0 && ix < slMarkers.Count ) {
			val =(double) slMarkers.GetKey( ix );
		}
		else {
			Console.WriteLine( "{0} GetKey doesn't have ix {1} {2}", Symbol, ix, slMarkers.Count );
			throw new Exception( "Pivot.GetKey" );
		}
		return val;
	}
	
	public int GetIndex( double val ) {
		int ix = -1;
		if ( slMarkers.ContainsKey( val ) ) {
			ix = slMarkers.IndexOfKey( val );
		}
		else {
			Console.WriteLine( "{0} GetIndex doesn't have val {1} {2}", Symbol, val, slMarkers.Count );
			throw new Exception( "Pivot GetIndex" );
			
		}
		return ix;
	}
	
	private void AddMarker( Marker marker ) {
		if ( slMarkers.ContainsKey( marker.Val ) ) {
			Marker m = (Marker) slMarkers[ marker.Val ];
			m.Count++;
		}
		else {
			slMarkers.Add( marker.Val, marker );
		}
	}
	
	public void Draw( ATSComponent atsc ) {
		
		for ( int i = slMarkers.Count - 1; i >= 0; i-- ) {
			Marker marker = (Marker) slMarkers.GetByIndex( i );
			atsc.Draw( marker.ds, 0 );
		}
	}
	
	public void SetOpeningTrade( double open ) {
		
		this.open = open;
		sixmonposcrossover += open;
		//Console.WriteLine( "sixmonposcrossover {0} {1}", Symbol, sixmonposcrossover );
		sixmonnegcrossover += open;
		//Console.WriteLine( "sixmonnegcrossover {0} {1}", Symbol, sixmonnegcrossover );
				
		sixmonposmean += open;
		sixmonnegmean += open;

		AddMarker( new Marker( "OPEN", open, Marker.EPivotType.Open, Marker.EPivotDuration.Day, 0, Color.Violet ) );

		AddMarker( new Marker( "+xvr", sixmonposcrossover, Marker.EPivotType.Xvr, Marker.EPivotDuration.Day, 0, Color.Cyan ) );
		AddMarker( new Marker( "-xvr", sixmonnegcrossover, Marker.EPivotType.Xvr, Marker.EPivotDuration.Day, 0, Color.Cyan ) );
				
		AddMarker( new Marker( "+mean", sixmonposmean, Marker.EPivotType.Mean, Marker.EPivotDuration.Day, 0, Color.DarkSalmon ) );
		AddMarker( new Marker( "-mean", sixmonnegmean, Marker.EPivotType.Mean, Marker.EPivotDuration.Day, 0, Color.DarkSalmon ) );
		
		double min = (double) slMarkers.GetKey( 0 );
		double max = (double) slMarkers.GetKey( slMarkers.Count - 1 );
		AverageDelta = ( max - min ) / ( slMarkers.Count - 1 );
		
		// *** make sorted list of pivots 
		// *** calculate average dif between pivots and use for trailing stop
			
		/// Console.WriteLine( "{0} step {1:0.00}", Symbol, AverageDelta );
		// use direction indicator at 0.25 of dif.

	}
	
	public void OnBar( Bar bar ) {
		
		for ( int i = slMarkers.Count - 1; i >= 0; i-- ) {
			Marker marker = (Marker) slMarkers.GetByIndex( i );
			marker.ds.Add( bar.DateTime, marker.Val );
		}
	}
}

public class VolumeAtPrice {
	
	SortedList slVolumeAtPrice;
	public int LargestVolume = 0;
	public double PriceAtLargestVolume = 0;
	
	public VolumeAtPrice() {
		slVolumeAtPrice = new SortedList( 400 );
	}
	
	public void Add( Trade trade ) {
		if ( slVolumeAtPrice.ContainsKey( trade.Price ) ) {
			int ix = slVolumeAtPrice.IndexOfKey( trade.Price );
			int volume = (int) slVolumeAtPrice.GetByIndex( ix );
			volume += trade.Size;
			slVolumeAtPrice.SetByIndex( ix, volume );
			if ( volume > LargestVolume ) {
				LargestVolume = volume;
				PriceAtLargestVolume = trade.Price;
			}
		}
		else {
			slVolumeAtPrice.Add( trade.Price, trade.Size );
			if ( trade.Size > LargestVolume ) {
				LargestVolume = trade.Size;
				PriceAtLargestVolume = trade.Price;
			}
		}
	}
}

public class Stats {
	
	double SumY = 0;
	double SumYY = 0;
	int Xcnt = 0;
	string Name;
	
	public Stats( String Name) {
		this.Name = Name;
	}
	
	public void Add( double val ) {

		SumY += val;
		SumYY += val * val;
		Xcnt++;
	}
		
	public void Report() {
		
		double Syy = SumYY - SumY * SumY / Xcnt;
		double Avg = SumY / Xcnt;
		double SD = Math.Sqrt( Syy / Xcnt );
		
		TimeSpan tsAvg = new TimeSpan( (int) Avg );
		TimeSpan tsSD = new TimeSpan( (int) SD );
		
		string t1 = tsAvg.ToString();
		string t2 = tsSD.ToString();
		Console.Write( "{0}: Cnt {1}, Avg {2:#.00}, SD {3:#.00}", Name, Xcnt, Avg, SD );
//		Console.Write( ", {0}, {1}", t1, t2 );
		Console.WriteLine();
	}
}

public class DirectionIndicator {
	
	double dblPatternDelta = 0.30; // pt1 becomes new anchor when abs(pt0-pt1)>delta
	int szPDD;
	double PatternPt0; // pattern end point, drags pt1 away from anchor, but can retrace at will
	double PatternPt1; // pattern mid point, can only move away from anchor point
	DateTime dtPatternPt1;  // when it was last encountered
	public DoubleSeries dsPattern;
	public enum EPatternState { init, start, down, up };
	EPatternState PatternState;
	// pt0, pt1 are set when first delta has been reached
	int[] rPatternDeltaDistance;
	public DoubleSeries dsPt0Ratio;
	double cntNewUp;
	double cntNewDown;
	public DoubleSeries dsSellDecisionPoint;
	public DoubleSeries dsBuyDecisionPoint;
	public bool bSellDecisionPointSet;
	public bool bBuyDecisionPointSet;
	
	DateTime dtTrendStart;
	DateTime dtRootPoint;
	int cntTrendPoints;
	
	Stats stats;
	Stats sectionstats;
	
	// Properties
	public EPatternState Direction {
		get { return PatternState; }
	}
	
	public double Limit {
		get { return PatternPt1; }
	}
	
	// Constructors
	public DirectionIndicator() {
		Init();
	}
	
	public DirectionIndicator( double Delta ) {
		this.dblPatternDelta = Delta;
		Init();
	}
	
	// Code
	private void Init() {
		
		stats = new Stats( "Pattern Points" );
		
		PatternState = EPatternState.init;
		PatternPt0 = 0;
		PatternPt1 = 0;
		dsPattern = new DoubleSeries( "Pattern" );
		dsPattern.Color = Color.Chocolate;
		dsPt0Ratio = new DoubleSeries( "Pt0 Ratio" );
		dsPt0Ratio.Color = Color.Chocolate;
		//Draw( dsOne, 8 );
		//Draw( dsHalf, 8 );
		//Draw( dsPt0Ratio, 8 );
		cntNewUp = 0;
		cntNewDown = 0;
		
		bSellDecisionPointSet = false;
		bBuyDecisionPointSet = false; 

		dsSellDecisionPoint = new DoubleSeries( "Sell Decision" );
		dsSellDecisionPoint.Color = Color.Red;
		dsSellDecisionPoint.DrawStyle = EDrawStyle.Circle;
		dsSellDecisionPoint.DrawWidth = 7;
		
		dsBuyDecisionPoint = new DoubleSeries( "Buy Decision" );
		dsBuyDecisionPoint.Color = Color.Blue;
		dsBuyDecisionPoint.DrawStyle = EDrawStyle.Circle;
		dsBuyDecisionPoint.DrawWidth = 7;
		
		szPDD = (int) Math.Round( dblPatternDelta * 100 );
		rPatternDeltaDistance = new int[ szPDD + 1 ]; 
		for ( int i = 0; i <= szPDD; i++ ) rPatternDeltaDistance[ i ] = 0;
		
	}
	
	public void Add( Quote quote ) {
		//
		// Pattern calculation 
		//
		double qHi;
		double qLo;
		qHi = Math.Max( quote.Ask, quote.Bid );
		qLo = Math.Min( quote.Ask, quote.Bid );
		double val = ( qHi + qLo ) / 2.0;
		double dif;
		
		bBuyDecisionPointSet = false;
		bSellDecisionPointSet = false;
		
		switch ( PatternState ) {
			case EPatternState.init:
				PatternPt1 = val;
				PatternPt0 = val;
				PatternState = EPatternState.start;
				dsPattern.Add( quote.DateTime, val );
				//Console.WriteLine( "{0} Pattern init {1}", SmartQuant.Clock.Now, PatternState );  
				break;
			case EPatternState.start:
				if ( Math.Abs( val - PatternPt1 ) >= dblPatternDelta ) {
					dtPatternPt1 = Clock.Now;
					PatternPt0 = val;
					if ( val > PatternPt1 ) {
						PatternState = EPatternState.up;
						// start collecting interpoint statistics
						dtRootPoint = quote.DateTime;
						sectionstats = new Stats( "di strt up " + dtPatternPt1.ToString() );
						//Console.WriteLine( "{0} Pattern start {1}", SmartQuant.Clock.Now, PatternState );  
					}
					else {
						PatternState = EPatternState.down;
						// start collecting interpoint statistics
						dtRootPoint = quote.DateTime;
						sectionstats = new Stats( "di strt dn " + dtPatternPt1.ToString() );
						//Console.WriteLine( "{0} Pattern start {1}", SmartQuant.Clock.Now, PatternState );  
					}
					PatternPt1 = val;
				}
				break;
			case EPatternState.up:
				val = quote.Bid;  // will need to sell on the bid
				PatternPt0 = val;
				if ( val > PatternPt1 ) {
					PatternPt1 = val;
					dtPatternPt1 = Clock.Now;
					cntNewUp++;
					dsSellDecisionPoint.Add( quote.DateTime, val );
					bSellDecisionPointSet = true;
					// update trend/inter-point statistics
					dif = (double) ( quote.DateTime.Ticks - dtRootPoint.Ticks );
					stats.Add( dif );
					sectionstats.Add( dif );
					dtRootPoint = quote.DateTime;
				}
				dif = PatternPt1 - PatternPt0;
				dsPt0Ratio.Add( quote.DateTime, dif / dblPatternDelta  );
				if ( dif >= dblPatternDelta ) {
					dsPattern.Add( dtPatternPt1, PatternPt1 );
					//mp.ClassifyDoubleSeriesEnd( dsPattern );
					if ( PatternPt1 > PatternPt0 ) {
						//Console.WriteLine( "{0} Pattern from {1}", SmartQuant.Clock.Now, PatternState );  
						PatternState = EPatternState.down;
						// close out trend statistics
						// start collecting interpoint statistics
						dtRootPoint = quote.DateTime;
						// **sectionstats.Report();
						sectionstats = new Stats( "di sctn dn " + dtPatternPt1.ToString() );
					}
					else {
						//Console.WriteLine( "{0} Pattern already {1}", SmartQuant.Clock.Now, PatternState );  
					}
				}
				else {
					rPatternDeltaDistance[ (int) Math.Round( dif * 100 ) ]++;
				}
				break;
			case EPatternState.down:
				val = quote.Ask;  // will need to buy on the ask
				PatternPt0 = val;
				if ( val < PatternPt1 ) {
					PatternPt1 = val;
					dtPatternPt1 = Clock.Now;
					cntNewDown++;
					dsBuyDecisionPoint.Add( quote.DateTime, val );
					bBuyDecisionPointSet = true; 
					// update trend/inter-point statistics
					dif = (double) ( quote.DateTime.Ticks - dtRootPoint.Ticks );
					stats.Add( dif );
					sectionstats.Add( dif );
					dtRootPoint = quote.DateTime;
				}
				dif = PatternPt0 - PatternPt1;
				dsPt0Ratio.Add( quote.DateTime, dif / dblPatternDelta );
				if ( dif >= dblPatternDelta ) {
					dsPattern.Add( dtPatternPt1, PatternPt1 );
					//mp.ClassifyDoubleSeriesEnd( dsPattern );
					if ( PatternPt1 < PatternPt0 ) {
						//Console.WriteLine( "{0} Pattern from {1}", SmartQuant.Clock.Now, PatternState );  
						PatternState = EPatternState.up;
						// close out trend statistics
						// start collecting interpoint statistics
						dtRootPoint = quote.DateTime;
						// **sectionstats.Report();
						sectionstats = new Stats( "di sctn up " + dtPatternPt1.ToString() );
					}
					else {
						//Console.WriteLine( "{0} Pattern already {1}", SmartQuant.Clock.Now, PatternState );  
					}
				}
				else {
					rPatternDeltaDistance[ (int) Math.Round( dif * 100 ) ]++;
				}
				break;
		}
	}

	public void Report() {
		
		if ( dsPattern.Count > 0 ) {
			double SumY = 0;
			double SumYY = 0;
			int Xcnt = 0;
			for ( int i = 1; i < dsPattern.Count; i++ ) {
				double val = Math.Abs( dsPattern[ i ] - dsPattern[ i - 1 ] );
				SumY += val;
				SumYY += val * val;
				Xcnt++;
			}
		
			double Syy = SumYY - SumY * SumY / Xcnt;
		
			Console.WriteLine( "{0} Pattern Segments Avg {1:#.00} SD {2:#.00}, Decision Points: up {3} down {4} dif {5}", 
				Xcnt, SumY / Xcnt, Math.Sqrt( Syy / Xcnt ), cntNewUp, cntNewDown, cntNewUp - cntNewDown );
			if ( dsPt0Ratio.Count > 0 ) {
				double mean = dsPt0Ratio.GetMean();
				double sd = dsPt0Ratio.GetStdDev();
				Console.WriteLine( "dsPt0Ratio avg {0:0.00} + sd {1:0.00} = {2:0.00}", mean, sd, mean + sd );
			}
		}
		else {
			Console.WriteLine( "No Data" );
		}
		
		//stats.Report();
		
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
		
	}
}

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
	public static double ttlActPL = 0;
	public static double ttlMaxPL = 0;
	public static int ttlCntPL = 0;
	public static bool bTotalEmitted = false;
	int Id = 0;
	
	enum State {
		Created, EntrySubmitted, EntryFilled, HardStopSubmitted, SoftStopSubmitted, ExitSubmitted, ExitFilled, Done
	}
	State state = State.Created;
	
	static RoundTrip() {
		Reset();
	}
	
	public RoundTrip( Instrument instrument ) {
		this.instrument = instrument;
		Id = ++id;
		bTotalEmitted = false;
	}
	
	public static void Reset() {
		ttlActPL = 0;
		ttlMaxPL = 0;
		ttlCntPL = 0;
		bTotalEmitted = false;
		id = 0;
	}
	
	public void Enter( Quote quote, SingleOrder order ) {
		latestQuote = quote;
		order.ExecutionReport += new ExecutionReportEventHandler(order_ExecutionReport);
		EntryOrder = order;
		if ( State.Created == state ) {
		}
		else {
			Console.WriteLine( "*** {0} RoundTrip.Enter in {1}", instrument.Symbol, state );
		}
		//Console.WriteLine( "setting entry submitted" );
		state = State.EntrySubmitted;
		dtEntryInitiation = quote.DateTime;
		quoteEntryInitiation = quote;
		if ( order.Side == SmartQuant.FIX.Side.Buy ) side = PositionSide.Long;
		if ( order.Side == SmartQuant.FIX.Side.Sell ) side = PositionSide.Short;
		// need an error condition here
	}
	
	public void HardStop( Quote quote, SingleOrder order ) {
		latestQuote = quote;
		order.ExecutionReport += new ExecutionReportEventHandler(order_ExecutionReport);
		ExitOrder = order;
		if ( State.EntryFilled == state ) {
		}
		else {
			Console.WriteLine( "*** {0} RoundTrip.HardStop in {1}", instrument.Symbol, state );
		}
		state = State.HardStopSubmitted;
		dtExitInitiation = quote.DateTime;
		quoteExitInitiation = quote;
	}
	
	public void SoftStop( Quote quote, SingleOrder order ) {
		latestQuote = quote;
		order.ExecutionReport += new ExecutionReportEventHandler(order_ExecutionReport);
		ExitOrder = order;
		if ( State.HardStopSubmitted == state ) {
		}
		else {
			Console.WriteLine( "*** {0} RoundTrip.SoftStop in {1}", instrument.Symbol, state );
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
		if ( State.EntryFilled == state || State.HardStopSubmitted == state  ) {
		}
		else {
			Console.WriteLine( "*** {0} RoundTrip.Exit in {1}", instrument.Symbol, state );
		}
		state = State.ExitSubmitted;
		dtExitInitiation = quote.DateTime;
		quoteExitInitiation = quote;
	}
	
	public void UpdateQuote( Quote quote ) {
		//Console.WriteLine(quote);
		latestQuote = quote;
		if ( State.EntryFilled == state || State.HardStopSubmitted == state ) {
			switch ( side ) {
				case PositionSide.Long:
					CurrentProfit = quote.Bid - EntryOrder.AvgPx;
					MaxProfit = Math.Max( MaxProfit, CurrentProfit );
					break;
				case PositionSide.Short:
					CurrentProfit = EntryOrder.AvgPx - quote.Ask;
					MaxProfit = Math.Max( MaxProfit, CurrentProfit );
					break;
			}
		}
	}
	
	public void Check( ) {
	}
	
	public void Report() {
		
		if ( ReportActive ) {
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
			switch ( side ) {
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
		
			ttlActPL += pl;
			ttlMaxPL += MaxProfit;
			ttlCntPL++;
		
      /*
			Console.Write( "rt,{0,2},{1},{2},", 
				Id, 
				quoteEntryInitiation.DateTime.ToString("HH:mm:ss.fff"),
				quoteEntryCompletion.DateTime.ToString("HH:mm:ss.fff")
				);
			Console.Write( "{0} {1}:{2,-5},{3},{4,3:#0.0},{5},{6,6:#0.00},{7,6:#0.00},",
				quoteExitCompletion.DateTime.ToString("HH:mm:ss.fff"),
				instrument.Symbol,
				side,
				sEntryDelay, sTripDuration, sExitDelay, MaxProfit, pl 
				);
		
			Console.WriteLine( ExitOrder.Text );
		*/
			ReportActive = false;
		}
		else {
			Console.WriteLine( "*** {0} Report called more than once.  Longs {1} Shorts {2}", TotalLong, TotalShrt );
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
		SingleOrder order = (SingleOrder) sender;
		
		/*
		Console.Write( "er '{0}',{1:#.00},{2},{3},{4},{5}",
		er.ClOrdID, er.AvgPx, er.Commission, er.CumQty, er.LastQty, er.OrderID  );
		Console.Write( ",{0},{1:#.00},{2},{3},{4}",
		er.OrdStatus, er.Price, er.Side, er.Tag, er.Text ); 
		Console.WriteLine(".");
		Console.WriteLine( "er State {0}", state );
		*/
		
		// a validation that our buy side ultimately matches our sell side
		switch ( order.Side ) {
			case Side.Buy:
				TotalLong += (int) Math.Round( er.LastQty );
				break;
			case Side.Sell:
				TotalShrt += (int) Math.Round( er.LastQty );
				break;
		}
		
		switch ( state ) {
			case State.Created:
				throw new Exception( "State.Create" );
				break;
			case State.EntrySubmitted:
				//throw new Exception( "State.EntrySubmitted" );
				switch ( er.OrdStatus ) {
					case OrdStatus.Filled:
						state = State.EntryFilled;
						dtEntryCompletion = latestQuote.DateTime;
						quoteEntryCompletion = latestQuote;
						switch ( side ) {
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
				switch ( er.OrdStatus ) {
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
// need order round trip  as well as the existing transaction round trip statistics
//

public class TransactionSetEventHolder {
	
	public delegate void UpdateQuoteHandler( object source, Quote quote );
	public event UpdateQuoteHandler OnUpdateQuote;
	
	public delegate void StrategyStopHandler( object source, EventArgs e );
	public event StrategyStopHandler OnStrategyStop;
	
	public delegate void UpdateSignalStatusHandler( object source, bool Exited );
	public event UpdateSignalStatusHandler UpdateSignalStatus;
	
	public void UpdateQuote( object source, Quote quote ) {
		if ( null != OnUpdateQuote ) OnUpdateQuote( source, quote );
	}
	
	public void StrategyStop( object source, EventArgs e ) {
		if ( null != OnStrategyStop ) OnStrategyStop( source, e );
	}
	
	public void SignalStatus( object source, bool Exited ) {
		if ( null != UpdateSignalStatus ) UpdateSignalStatus( source, Exited );
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
	double TrailingStopDelta;
	Instrument instrument;
	string Symbol;
	int OrderSize;
	
	//string HardStopClOrdID = "";  // track our existing stop order
	double HardStopPrice;
	double SoftStopPrice;
	double AvgEntryPrice;  // price at which the entry filled
	
	bool OutStandingOrdersExist = false;
	
	int PositionRequested = 0;  // positive for long negative for short
	int PositionFilled = 0;		// positive for long negative for short
	
	RoundTrip trip;
	
	ATSComponent atsc;
	TrackOrdersLink OrderLink;
	
	public TransactionSet( ATSComponent atsc ) {
		this.atsc = atsc;
	}
	
	public void Create( 
		ESignal Signal, TrackOrdersLink OrderLink,
		int OrderSize,
		Quote quote, double JumpDelta, 
		double HardStopPrice, double TrailingStopDelta,
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
		this.HardStopPrice = HardStopPrice;
		this.TrailingStopDelta = TrailingStopDelta;
		this.quote = quote;
		this.OrderLink = OrderLink;
		instrument = atsc.Instrument;
		Symbol = instrument.Symbol;
		this.OrderSize = OrderSize;
		
		trip = new RoundTrip( instrument );
		
		ordersSubmitted = new Hashtable(10);
		ordersPartiallyFilled = new Hashtable(10);
		ordersFilled = new Hashtable(10);
		ordersCancelled = new Hashtable(10); 
		ordersRejected = new Hashtable(10);
		ordersTag = new Hashtable(10);
		
		if ( !(( ESignal.Long == Signal ) || ( ESignal.Short == Signal )) ) {
			Console.WriteLine( "Transaction Set Problem 1" );
			throw new ArgumentException( instrument.Symbol +  " has improper Entry Signal: " + Signal.ToString() );
		}
		
		SingleOrder order;
		
		switch ( Signal ) {
			case ESignal.Long:
				order = atsc.MarketOrder( SmartQuant.FIX.Side.Buy, OrderSize );
				//entryOrder = new LimitOrder(instrument, SmartQuant.FIX.Side.Buy, Quantity, 4.55 );
				//order = atsc.StopOrder( SmartQuant.FIX.Side.Buy, quanInitial, Math.Round( quote.Ask + JumpDelta, 2 ) );
				order.Text = "Long Mkt Entr";
				ordersTag.Add( order.ClOrdID, EOrderTag.Entry );
				PositionRequested = OrderSize;
				State = EState.EntrySent;
				trip.Enter( quote, order );
				SendOrder( quote, order );
				break;
			case ESignal.Short:
				order = atsc.MarketOrder( SmartQuant.FIX.Side.Sell, OrderSize );
				//entryOrder = new LimitOrder(instrument, SmartQuant.FIX.Side.Buy, Quantity, 4.55 );
				//order = atsc.StopOrder( SmartQuant.FIX.Side.Sell, quanInitial, Math.Round( quote.Bid - JumpDelta, 2 ) );
				order.Text = "Shrt Mkt Entr";
				ordersTag.Add( order.ClOrdID, EOrderTag.Entry );
				PositionRequested = -OrderSize;
				State = EState.EntrySent;
				trip.Enter( quote, order );
				SendOrder( quote, order );
				break;
		}
	}
	
	private void SendOrder( Quote quote, SingleOrder order ) {
		order.ExecutionReport += new ExecutionReportEventHandler(order_ExecutionReport);
		order.StatusChanged += new EventHandler(order_StatusChanged);
		OutStandingOrdersExist = true;
		ordersSubmitted.Add( order.ClOrdID, order );
		order.Send();
		//Console.WriteLine( "{0} {1} sent", order.ClOrdID, order.Text );
	}
	
	private void SetHardStop() {
		// Submit a hardstop order for protection
		SingleOrder order;
		switch ( EntrySignal ) {
			case ESignal.Long:
				order = atsc.StopOrder( SmartQuant.FIX.Side.Sell, Math.Abs( PositionFilled ), HardStopPrice );
				order.Text = "Long Hard Stop";
				ordersTag.Add( order.ClOrdID, EOrderTag.HardStop );
				//PositionRequested = -Math.Abs( PositionFilled );
				State = EState.WaitForExit;
				trip.HardStop( quote, order );
				SendOrder( quote, order );
				break;
			case ESignal.Short:
				order = atsc.StopOrder( SmartQuant.FIX.Side.Buy, Math.Abs( PositionFilled ), HardStopPrice );
				order.Text = "Shrt Hard Stop";
				ordersTag.Add( order.ClOrdID, EOrderTag.HardStop );
				//PositionRequested = Math.Abs( PositionFilled );
				State = EState.WaitForExit;
				trip.HardStop( quote, order );
				SendOrder( quote, order );
				break;
		}
	}
	
	private void CheckSoftStop() {
		// don't update if we have no position
		SingleOrder order;
		if ( EState.WaitForExit == State && ( 0 != PositionFilled && trip.MaxProfit > 0.00 ) ) {
			double t;
			switch ( EntrySignal ) {
				case ESignal.Long:
					//t = quote.Bid - TrailingStopDelta;
					//if ( t > SoftStopPrice ) {
					//	SoftStopPrice = t;
					//}
					//if ( quote.Bid < SoftStopPrice ) {
					//if ( trip.MaxProfit > 0.06 ) {
					if ( trip.MaxProfit > 20.06 ) {
						CancelSubmittedOrders();
						order = atsc.MarketOrder( SmartQuant.FIX.Side.Sell, Math.Abs( PositionFilled ) );
						order.Text = "Long Mkt Stop";
						ordersTag.Add( order.ClOrdID, EOrderTag.TrailingStop );
						PositionRequested -= Math.Abs( PositionFilled );
						State = EState.CleanUp ;
						trip.SoftStop( quote, order );
						SendOrder( quote, order );
					}
					break;
				case ESignal.Short:
					//t = quote.Ask + TrailingStopDelta;
					//if ( t < SoftStopPrice ) {
					//	SoftStopPrice = t;
					//}
					//if ( quote.Ask > SoftStopPrice ) {
					//if ( trip.MaxProfit > 0.06 ) {
					if ( trip.MaxProfit > 20.06 ) {
						CancelSubmittedOrders();
						order = atsc.MarketOrder( SmartQuant.FIX.Side.Buy, Math.Abs( PositionFilled ) );
						order.Text = "Shrt Mkt Stop";
						ordersTag.Add( order.ClOrdID, EOrderTag.TrailingStop );
						PositionRequested += Math.Abs( PositionFilled );
						State = EState.CleanUp ;
						trip.SoftStop( quote, order );
						SendOrder( quote, order );
					}
					break;
			}
		}
	}
	
	void CancelSubmittedOrders() {
		if ( 0 < ordersSubmitted.Count ) {
			//Console.WriteLine( "Cancelling {0} orders", ordersSubmitted.Count );
			
			// Not sure if this is the ideal place, but need to remove transaction set from order list
			try { 
				OrderLink.Link.Remove( OrderLink.ID );
			}
			catch ( Exception e ) {
        //Console.WriteLine("CancelSubmittedOrders " + e);
			}
			
			Queue q = new Queue( 10 );
			foreach ( SingleOrder order in ordersSubmitted.Values ) {
				q.Enqueue( order );
			}
			while ( 0 != q.Count ) {
				SingleOrder order = (SingleOrder) q.Dequeue();
				//Console.WriteLine( "{0} cancelling", order.ClOrdID );
				order.Cancel();
			}
		}
	}
	
	public void UpdateSignal( ESignal Signal ) {
		//Console.WriteLine( "In UpdateSignal" );
		if ( this.EntrySignal == Signal ) {
			// don't bother with stuff in the same direction, just keep monitoring the situation
		}
		else {
			switch ( Signal ) {
				case ESignal.Long:
				case ESignal.Short:
					throw new ArgumentException( 
						instrument.Symbol + " has improper Update Signal: " 
						+ Signal.ToString() + " vs " + EntrySignal.ToString() );
					break;
				case ESignal.ScaleIn:
					break;
				case ESignal.ScaleOut:
					break;
				case ESignal.Exit:
					//Console.WriteLine( "UpdateSignal {0} {1} {2} {3}", Signal, State, PositionRequested, PositionFilled );
					if ( EState.WaitForExit == State || EState.EntrySent == State ) {
						// cancel all outstanding orders
						CancelSubmittedOrders();
						// set flag so that if something gets filled, to unfill it right away 
						// later may want to keep it if things are going in the correct direction
						SingleOrder order;
						if ( 0 < PositionFilled ) {
							order = atsc.MarketOrder( SmartQuant.FIX.Side.Sell, Math.Abs( PositionFilled ) );
							//entryOrder = new LimitOrder(instrument, SmartQuant.FIX.Side.Buy, Quantity, 4.55 );
							//entryOrder = new StopOrder(instrument, SmartQuant.FIX.Side.Sell, Quantity, quote.Bid - Jump );
							order.Text = "Long Mkt Exit";
							ordersTag.Add( order.ClOrdID, EOrderTag.Exit );
							PositionRequested -= Math.Abs( PositionFilled );
							State = EState.CleanUp;
							trip.Exit( quote, order );
							SendOrder( quote, order );
						}
						if ( 0 > PositionFilled ) {
							order = atsc.MarketOrder(SmartQuant.FIX.Side.Buy, Math.Abs( PositionFilled ) );
							//entryOrder = new LimitOrder(instrument, SmartQuant.FIX.Side.Buy, Quantity, 4.55 );
							//entryOrder = new StopOrder(instrument, SmartQuant.FIX.Side.Buy, Quantity, quote.Ask + Jump );
							order.Text = "Shrt Mkt Exit";
							ordersTag.Add( order.ClOrdID, EOrderTag.Exit );
							PositionRequested += Math.Abs( PositionFilled );
							State = EState.CleanUp;
							trip.Exit( quote, order );
							SendOrder( quote, order );
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
		
		switch ( order.OrdStatus ) {
			case OrdStatus.PartiallyFilled:
				if ( 0 != order.LeavesQty ) { 
					Console.WriteLine("*** {0} Remaining quantity = {1}", instrument.Symbol, order.LeavesQty);
				}
				if ( !ordersPartiallyFilled.ContainsKey( order.ClOrdID ) ) {
					if ( ordersSubmitted.ContainsKey( order.ClOrdID ) ) {
						ordersSubmitted.Remove( order.ClOrdID );
					}
					ordersPartiallyFilled.Add( order.ClOrdID, order );
				}
				//CheckStop = true;  // need to fix this sometime
				break;
			case OrdStatus.Filled:
				//Console.WriteLine("Average fill price = {0}@{1:#.00} {2} {3}", order.OrderQty, order.AvgPx, order.Side, order.OrdStatus);
				if ( ordersSubmitted.ContainsKey( order.ClOrdID ) ) {
					ordersSubmitted.Remove( order.ClOrdID );
				}
				if ( ordersPartiallyFilled.ContainsKey( order.ClOrdID ) ) {
					ordersPartiallyFilled.Remove( order.ClOrdID );
				}
				ordersFilled.Add( order.ClOrdID, order );
				CheckStop = true;  // HardStopPrice on set on 'Filled'
				break;
			case OrdStatus.Cancelled:
				if ( ordersSubmitted.ContainsKey( order.ClOrdID ) ) {
					ordersSubmitted.Remove( order.ClOrdID );
				}
				ordersCancelled.Add( order.ClOrdID, order );
				break;
			case OrdStatus.PendingCancel:
				// not used during simulation
				// signalled during realtime trading
				break;
			default:
				Console.WriteLine("*** {0} Order status changed to : {1}", instrument.Symbol, order.OrdStatus.ToString());
				break;
		}
		
		if ( CheckStop ) {
			if ( ordersTag.ContainsKey( order.ClOrdID ) ) {
				EOrderTag tag = (EOrderTag) ordersTag[ order.ClOrdID ];
				if ( EOrderTag.Entry == tag ) {
					SetHardStop();
				}
				if ( EOrderTag.HardStop == tag ) {
					State = EState.CleanUp;
				}
			}
		}

		OutStandingOrdersExist = ( ( 0 != ordersSubmitted.Count ) || ( 0 != ordersPartiallyFilled.Count ) );
		//Console.WriteLine( "{0} status {1} {2} {3} {4}", order.ClOrdID, order.OrdStatus, 
		//	OutStandingOrdersExist, ordersSubmitted.Count, ordersPartiallyFilled.Count );
	}

	void order_ExecutionReport( object sender, ExecutionReportEventArgs args ) {
		
		SingleOrder order = sender as SingleOrder;
		ExecutionReport report = args.ExecutionReport; 
		
		//Console.WriteLine("Execution report type : " + report.ExecType);

		if ( report.ExecType == ExecType.Fill || report.ExecType == ExecType.PartialFill ) {
			//Console.WriteLine("Fill report, average fill price = {0}@{1:#.00}", report.OrderQty, report.AvgPx);
			//Console.WriteLine( "*** {0} Report {1} cum {2} leaves {3} last {4} total {5} ",
			//	instrument.Symbol, report.OrdStatus, report.CumQty, report.LeavesQty, report.LastQty, report.OrderQty );
			switch ( order.Side ) {
				case Side.Buy:
					PositionFilled += (int) Math.Round( report.LastQty ); 
					break;
				case Side.Sell:
					PositionFilled -= (int) Math.Round( report.LastQty ); 
					break;
			}
		}
		if ( report.ExecType == ExecType.Fill ) {
			if ( ordersTag.ContainsKey( order.ClOrdID ) ) {
				EOrderTag tag = (EOrderTag) ordersTag[ order.ClOrdID ];
				if ( EOrderTag.Entry == tag ) {
					AvgEntryPrice = report.AvgPx;
					switch ( EntrySignal ) {
						case ESignal.Long:
							//HardStopPrice = AvgEntryPrice - HardStopDelta;  // this may not work if we have multiple partial fills
							SoftStopPrice = AvgEntryPrice - TrailingStopDelta;
							break;
						case ESignal.Short:
							//HardStopPrice = AvgEntryPrice + HardStopDelta;  // this may not work if we have multiple partial fills
							SoftStopPrice = AvgEntryPrice + TrailingStopDelta;
							break;
					}
				}
			}
		}
	}
	
	void OnStrategyStop( object o, EventArgs e ) {
		//Console.WriteLine( "{0} TransactionSet StrategyStop", instrument.Symbol );
		UpdateSignal( ESignal.Exit );
		ClearEvents();
	}

	private void OnUpdateQuote( object source, Quote quote ) {
		//Console.WriteLine( "In UpdateQuote" );
		this.quote = quote;
		trip.UpdateQuote( quote );
		CheckSoftStop();
		//Console.WriteLine( "updatequote {0} {1} {2} {3}", State, OutStandingOrdersExist, ordersSubmitted.Count, ordersPartiallyFilled.Count );
		if ( EState.CleanUp == State && !OutStandingOrdersExist ) {
			//if ( !OutStandingOrdersExist ) {
			//Console.WriteLine( "transaction set final clean up" );
			ClearEvents();
			eventholder.SignalStatus( this, true );
			State = EState.Done;
		}
	}
	
	void ClearEvents() {
		if ( UpdateQuoteActive ) {
			eventholder.OnUpdateQuote -= OnUpdateQuote;
			UpdateQuoteActive = false;
		}
		if ( OnStrategyStopEventActive ) {
			eventholder.OnStrategyStop -= OnStrategyStop;
			OnStrategyStopEventActive = false;
		}
	}
}

#endregion TransactionSet

public class TrackOrdersLink {
	// used to pass in a TrackOrders object and the transaction key
	
	TrackOrders to;
	int Id;
	
	public TrackOrders Link {
		get { return to; }
	}
	
	public int ID {
		get { return Id; }
		}
	
	public TrackOrdersLink( TrackOrders to, int Id ) {
		this.to = to;
		this.Id = Id;
	}
}

public class TrackOrders {
	
	int cntOrders = 0;
	int lastId = 0;
	//LinkedList<TransactionSet> llTransactions;
	SortedList slTransactions;
	
	public int Count {
		get { return cntOrders; }
	}
	
	public int NewOrderId {
		get { return ++lastId; }
	}
	
	public TrackOrders() {
		//llTransactions = new LinkedList<TransactionSet>();
		slTransactions = new SortedList( 50 );
	}
	
	public int AddBuy( TransactionSet trans ) {
		//llTransactions.AddLast( new LinkedListNode<TransactionSet>( trans ) );
		int Id = NewOrderId;
		slTransactions.Add( Id, trans );
		cntOrders++;
		return Id;
	}
	
	public int AddSell( TransactionSet trans ) {
		int Id = NewOrderId;
		//llTransactions.AddLast( new LinkedListNode<TransactionSet>( trans ) );
		slTransactions.Add( Id, trans );
		cntOrders--;
		return Id;
	}
	
	public TransactionSet Remove() {
		
		return Remove( (int) slTransactions.GetKey( 0 ) );
	}
	
	public TransactionSet Remove( int Id ) {
		//LinkedListNode<TransactionSet> node = llTransactions.First;
		//TransactionSet trans = node.Value;
		//llTransactions.RemoveFirst();
		if ( 0 == cntOrders ) {
			throw new Exception( "TransactionSet Remove has 0 orders for Id " + Id.ToString() );
		}
		TransactionSet trans = (TransactionSet) slTransactions[ Id ];
		slTransactions.Remove( Id );
		if ( cntOrders > 0 ) cntOrders--;
		else cntOrders++;
		return trans;
	}
}

public class TrackTarget {
	// shared between Indicator and Transaction
	//   Indicator updates with current setting
	//   Transaction tracks it for exit
	
	private bool _Changed;
	private double _Value;
	
	public TrackTarget() {
		_Changed = false;
	}
	
	public double Value {
		get { 
			_Changed = false;
			return _Value; 
		}
		set {
			_Value = value;
			_Changed = true;
			
		}
	}
	
	public bool Changed {
		get { return _Changed; }
	}
}

public class ER {
	// Efficiency Ratio, Kaufman, page 732
	
	private bool Primed;
	private double First;
	private double Last;
	private double Sum;
	
	public ER() {
		Clear();
	}
	
	public void Add( double Value ) {
		if ( Primed ) {
			Sum += Math.Abs( Value - Last );
			Last = Value;
		}
		else {
			First = Value;
			Last = Value;
			Primed = true;
		}
	}
	
	public double Ratio {
		get {
			return Sum == 0 ? 0 : ( Math.Abs( Last - First ) / Sum );
		}
	}
	
	public void Clear() {
		Primed = false;
		Sum = 0;
	}
}

public class ValueAtTime {
	public DateTime DateTime;
	public double Value;
	
	public ValueAtTime( DateTime DateTime, double Value ) {
		this.DateTime = DateTime;
		this.Value = Value;
	}
}

public class SlidingWindow {
	
	protected LinkedList<ValueAtTime> values;
	protected int WindowWidthSeconds = 1000000;
	protected int WindowWidthCount   = 1000000;
	
	protected TimeSpan tsWindow;
	protected DateTime dtLast;
	
	public SlidingWindow( int Seconds ) {
		WindowWidthSeconds = Seconds;
		Init();
	}
	
	public SlidingWindow( int Seconds, int Count ) {
		WindowWidthSeconds = Seconds;
		WindowWidthCount   = Count;
		Init();
	}
	
	private void Init() {
		values = new LinkedList<ValueAtTime>();
		tsWindow = new TimeSpan( 0, 0, WindowWidthSeconds ); // WindowSize is in seconds
	}
	
	public virtual void Add( DateTime dt, double val ) {
		values.AddLast( new ValueAtTime( dt, val ) );
		dtLast = dt;
	}
	
	public virtual void Remove( DateTime dt, double val ) {
	}
	
	public virtual void CheckWindow() {
		bool Done;
		if ( 0 < values.Count ) {
			DateTime dtPurgePrior = dtLast - tsWindow;
			
			// Time based decimation
			Done = false;
			while ( !Done ) {
				ValueAtTime vat = values.First.Value;
				DateTime dtFirst = vat.DateTime;
				if ( vat.DateTime < dtPurgePrior ) {
					Remove( vat.DateTime, vat.Value );
					values.RemoveFirst();
					if ( 0 == values.Count ) Done = true; 
				}
				else {
					Done = true;
				}
			}
			
			// Count based decimation
			while ( WindowWidthCount < values.Count ) {
				ValueAtTime vat = values.First.Value;
				Remove( vat.DateTime, vat.Value );
				values.RemoveFirst();
			}
		}
	}
	
}

#region RunningMinMax

public class PointStat {
	
	public int PriceCount = 0;  // # of objects at this price point
	public int PriceVolume = 0;  // how much volume at this price point
	
	public PointStat() {
		PriceCount++;
	}
}

public class RunningMinMax: SlidingWindow {
	
	// basically keeps track of Min and Max value over selected duration
	
	public double Max;
	public double Min;
	
	private double prvMax;
	private double prvMin;
	private bool prvPrimed;
	private bool Changed;

  public bool MaxRising;
  public bool MaxFalling;
  public bool MinRising;
  public bool MinFalling;

	public enum EMinMax { Rising, Steady, Falling };
	public EMinMax MaxStatus;
	public EMinMax MinStatus;
	
	protected SortedList slPoints;  // holds array of stats per price point
	protected int PointCount;

	public RunningMinMax( int Seconds ): base( Seconds ) {

		slPoints = new SortedList(500);

		MaxStatus = EMinMax.Steady;
		MinStatus = EMinMax.Steady;

    Changed = false;
    prvPrimed = false;

    MaxRising = false;
    MaxFalling = false;
    MinRising = false;
    MinFalling = false;

    Max = 0;
    Min = 0;
    prvMax = 0;
    prvMin = 0;

    PointCount = 0;
	}

	public override void Add( DateTime dt, double val ) {
		
		base.Add( dt, val );
		if ( slPoints.ContainsKey(val) ) {
			PointStat ps = (PointStat) slPoints[ val ];
			ps.PriceCount++;
		}
		else {
			slPoints.Add( val, new PointStat() );
			Max = (double) slPoints.GetKey(slPoints.Count - 1);
			Min = (double) slPoints.GetKey(0);
			Changed = true;
			if ( !prvPrimed ) {
				prvMax = Max;
				prvMin = Min;
				prvPrimed = true;
			}
		}
		PointCount++;
	}
	
	public override void Remove( DateTime dt, double val ) {
		Remove( val );
	}
	
	private void Remove( double val ) {
		if ( slPoints.ContainsKey( val ) ) {
			PointStat ps = (PointStat) slPoints[ val ];
			ps.PriceCount--;
			if ( 0 == ps.PriceCount ) {
				slPoints.Remove(val);
				if ( 0 < slPoints.Count ) {
					Min = (double) slPoints.GetKey(0);
					Max = (double) slPoints.GetKey(slPoints.Count - 1);
					Changed = true;
					//Console.Write( "  Min {0:#.00} Max {1:#.00}", Min, Max );
				}
			}
			PointCount--;
		}
		else {
			throw new Exception("slPoints doesn't have a point to remove" );
		}
	}
	
	public override void CheckWindow() {
		base.CheckWindow();
		if ( Changed ) {
			if ( Max == prvMax ) {
				//MaxStatus = EMinMax.Steady;
			}
			else {
				if ( Max > prvMax ) {
					MaxStatus = EMinMax.Rising;
          MaxRising = true;
          MaxFalling = false;
					//if ( EMinMax.Falling == MinStatus ) MinStatus = EMinMax.Steady;
				}
				else {
          MaxRising = false;
          MaxFalling = true;
					MaxStatus = EMinMax.Falling;
				}
			}
			if ( Min == prvMin ) {
				//MinStatus = EMinMax.Steady;
			}
			else {
				if ( Min > prvMin ) {
					MinStatus = EMinMax.Rising;
          MinRising = true;
          MinFalling = false;
				}
				else {
					//if ( EMinMax.Rising == MaxStatus ) MaxStatus = EMinMax.Steady;
					MinStatus = EMinMax.Falling;
          MinRising = false;
          MinFalling = true;
				}
			}
			prvMax = Max;
			prvMin = Min;
		}
	}
	
}

#endregion RunningMinMax

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
	
	public double RR;
	public double R;
	
	public double SD;
	
	public int Xcnt = 0;
	
	private bool CanCalcSlope = false;
	
	public RunningStats() {
	}

	public void Add( double x, double y ) {
		//Console.WriteLine( "add,{0},{1:#.00},{2:#.000}", dsSlope.Name, val, x );
		SumXX += x * x;
		SumX  += x;
		SumXY += x * y;
		SumY  += y;
		SumYY += y * y;
		Xcnt++;
	}
	
	public void Remove( double x, double y ) {
		//Console.WriteLine( "rem,{0},{1:#.00},{2:#.000}", dsSlope.Name, val, x );
		SumXX -= x * x;
		SumX  -= x;
		SumXY -= x * y;
		SumY  -= y;
		SumYY -= y * y;
		Xcnt--;
		
		CanCalcSlope = true;
	}
	
	public void Calc() {
		
		double oldb1 = b1;
		
		Sxx = SumXX - SumX * SumX / Xcnt;
		Sxy = SumXY - SumX * SumY / Xcnt;
		Syy = SumYY - SumY * SumY / Xcnt;

		SST = Syy;
		SSR = Sxy * Sxy / Sxx;
		SSE = SST - SSR;

		RR = SSR / SST;
		R = Sxy / Math.Sqrt(Sxx * Syy);
			
		//if ( Xcnt < 2 ) Xcnt = 2;
		//if ( Xcnt < 1 ) Console.WriteLine( "Xcnt {0}", Xcnt );
		SD = Math.Sqrt( Syy / ( Xcnt - 1 ) );
		//SD = Math.Sqrt( Syy / ( Xcnt ) );

		meanY = SumY / Xcnt;
		
		b1 = CanCalcSlope ? Sxy / Sxx : 0;
		b0 = ( 1 / Xcnt ) * ( SumY - b1 * SumX );
		b2 = b1 - oldb1;  // *** do this differently
	}
}

#endregion RunningStats

#region AccumulationGroup

#region Accumulation

public class Accumulation: SlidingWindow {
	
	Color color;
	
	protected long firstTimeTick = 0;  // use as offset, or bias for time calc in 

	protected RunningStats stats;
	
	public DoubleSeries dsSlope;
	public DoubleSeries dsRR;
	public DoubleSeries dsAvg;
	
	public TrackTarget targetBBAvg;
	
	protected Accumulation enclosingAccumulation = null;

	public Accumulation EnclosingAccumulation{
		set { enclosingAccumulation = value; }
	}
	
	public Accumulation( 
		string Name, Color color, int WindowSizeTime, int WindowSizeCount ): base( WindowSizeTime, WindowSizeCount )
		{
		
		this.color = color;
		stats = new RunningStats();
		
		dsSlope = new DoubleSeries( "b1 " + Name );
		dsSlope.Color = color;
		
		dsRR = new DoubleSeries( "rr " + Name );
		dsRR.Color = color;
		
		dsAvg = new DoubleSeries( "avg " + Name );
		dsAvg.SecondColor = Color.Purple;
		dsAvg.Color = color;
		targetBBAvg = new TrackTarget();
		
	}
	
	public override void Add( DateTime dt, double val ) {
		base.Add( dt, val );
		double t = (double) ( dt.Ticks - firstTimeTick ) / ( (double) TimeSpan.TicksPerSecond );
		stats.Add( t, val );
	}
	
	public override void Remove( DateTime dt, double val ) {
		double t = (double) ( dt.Ticks - firstTimeTick ) / ( (double) TimeSpan.TicksPerSecond );
		stats.Remove( t, val );
	}
	
	protected void CalcStats() {
		stats.Calc();
		// //dsAccel.Add( dtLast, b2 * 10000.0 );
		//dsSlope.Add(dtLast, stats.b1 );
		//dsRR.Add( dtLast, stats.RR );
		// //dsSD.Add( dtLast, SD );
		dsAvg.Add( dtLast, stats.meanY );
		targetBBAvg.Value = stats.meanY;
	}
}

public class AccumulateValues: Accumulation {
	
	public AccumulateValues( string Name, Color color, int WindowSizeTime ) :
	base( Name, color, WindowSizeTime, 100000 ) {
	}

	public override void Add( DateTime dt, double val ) {
		if ( 0 == values.Count ) {
			firstTimeTick = dt.Ticks;
		}
		base.Add(dt, val );
		CheckWindow();
		CalcStats();
	}
}

public class AccumulateQuotes: Accumulation {
	
	//private QuoteArray quotes;
	private TimeSpan ms;
	private DateTime dtUnique;
	
	public bool CalcTrade;
	
	protected double BBMultiplier;
	public DoubleSeries dsBBUpper;
	public DoubleSeries dsBBLower;
	public DoubleSeries dsB;
	public DoubleSeries dsBandwidth;
	
	public TrackTarget targetBBUpr;
	public TrackTarget targetBBLwr;
	
	//public RunningMinMax minmax;
	//public DoubleSeries dsNormalizedMinMax;
	
	private double m_bbwMin = 0;
	private double m_bbwMax = 0;
	
	public AccumulateQuotes( string Name, int WindowSizeTime, int WindowSizeCount, 
		double BBMultiplier, bool CalcTrade, Color color) : 
		base( Name, color, WindowSizeTime, WindowSizeCount ) {
		
		this.CalcTrade = CalcTrade;
		ms = new TimeSpan( 0, 0, 0, 0, 1 );
	
		this.BBMultiplier = BBMultiplier;
		
		// see page 157 in Bollinger on Bollinger Bands
		
		//minmax = new RunningMinMax( WindowSizeTime / 2 );
		//dsNormalizedMinMax = new DoubleSeries( "bbx " + Name );
		//dsNormalizedMinMax.Color = color;

		dsBBUpper = new DoubleSeries( "bbu " + Name );
		dsBBUpper.Color = color;
		targetBBUpr = new TrackTarget();
		
		dsBBLower = new DoubleSeries( "bbl " + Name );
		dsBBLower.Color = color;
		targetBBLwr = new TrackTarget();
		
		dsB = new DoubleSeries( "%b " + Name );
		dsB.Color = color;
		
		dsBandwidth = new DoubleSeries( "bbw " + Name );
		dsBandwidth.Color = color;
		
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
		
		if ( 0 == values.Count ) {
			dtUnique = quote.DateTime;
			firstTimeTick = quote.DateTime.Ticks;
		}
		else {
			dtUnique = ( quote.DateTime > dtUnique ) ? quote.DateTime : dtUnique + ms;
		}
		double midpoint = ( quote.Bid + quote.Ask ) / 2;
		base.Add( dtUnique, quote.Bid );
		base.Add( dtUnique, quote.Ask );
		Process( midpoint );
		//minmax.Add( dtUnique, quote.Bid );
		//minmax.Add( dtUnique, quote.Ask );
		//minmax.Add( dtUnique, midpoint );
		//minmax.CheckWindow();
		//double denominator = minmax.Max - minmax.Min;
		//if ( denominator > 0 ) {
			//double normalized = ( midpoint - minmax.Min ) / ( denominator );
			//dsNormalizedMinMax.Add( dtUnique, normalized );
		//}
		
	}
	
	public override void Add( DateTime dt, double val ) {
	
		if ( 0 == values.Count ) {
			dtUnique = dt;
			firstTimeTick = dt.Ticks;
		}
		else {
			dtUnique = ( dt > dtUnique ) ? dt : dtUnique + ms;
		}
		base.Add( dtUnique, val );
		Process( val );
	}
		
	private void Process( double tmp ) {
		
		//Console.WriteLine( "{0} Added {1:#.00} {2:#.00} {3:#.00} {4}", dt, val, Min, Max, dsPoints.Count );
		
		CheckWindow();
		CalcStats();
		
		if ( !double.IsNaN( stats.b1 ) && !double.IsPositiveInfinity( stats.b1 ) && !double.IsNegativeInfinity( stats.b1 ) ) {
				
			//slopeAvg.Add( dtUnique, meanY );
			
			double upper = stats.meanY + BBMultiplier * stats.SD;
			double lower = stats.meanY - BBMultiplier * stats.SD;
			dsBBUpper.Add( dtUnique, upper );
			targetBBUpr.Value = upper;
			dsBBLower.Add( dtUnique, lower );
			targetBBLwr.Value = lower;
			
			//double tmp = ( quote.Bid + quote.Ask ) / 2;
			double avgquote = 0;
			/*
			if ( tmp == stats.meanY ) {
				avgquote = tmp;
			}
			else {
				if ( tmp > stats.meanY ) avgquote = quote.Ask;
				if ( tmp < stats.meanY ) avgquote = quote.Bid;
			}
			*/
			avgquote = tmp;  // instead of what is comments
			dsB.Add( dtUnique, ( avgquote - lower ) / ( upper - lower ) );
			double bw = 1000.0 * ( upper - lower ) / stats.meanY;
			double bbw = bw / m_bbwMax;
			dsBandwidth.Add( dtUnique, bbw );

			//minmax.Add( dtUnique, bbw );
			//minmax.CheckWindow();
			
		}
		//Console.WriteLine( "{0} Added {1} {2} {3} {4} {5}", dtUnique, Xcnt, b1, b0, R, dsSlope.Name );
	
		

	}
	
}

#endregion Accumulation

#endregion AccumulationGroup

[StrategyComponent("{55e7fcd2-3e34-4218-8d01-571c6c54fb4d}", ComponentType.ATSComponent , Name="atscPivot", Description="")]
public class atscPivot : ATSComponent {

#region stratVars
	
	int BarWidth = 20;  // seconds
	bool FirstSessionTradeFound = false;
	Quote latestQuote;
	
	Pivots pivots;
	
	DoubleSeries dsBid;
	DoubleSeries dsBidVolume;
	DoubleSeries dsAsk;
	DoubleSeries dsAskVolume;
	Stats statsSpread;
	
	AccumulateValues avgQuote;
	
	public AccumulateQuotes[] rAccum;

	DoubleSeries dsTrade;
	DoubleSeries dsTradeVolume;
	
	VolumeAtPrice vap;
	DoubleSeries dsVolumeAtPrice;
	
	bool CanTrade = false;  // performance window doesn't work properly if trades generated before trade enters system

	DateTime dtStartingDate;
	DateTime dtEndingDate;
	DateTime dtRunningStart;

  public bool ExitSignal;
  public bool LongEnter;
  public bool LongExit;
  public bool ShortEnter;
  public bool ShortExit;

  public TimeSpan SessionBegin;
  public TimeSpan SessionEnd;
  public TimeSpan TradingTimeBegin;
  public TimeSpan TradingTimeEnd;
  public TrackOrders orders;

  public double Default = 0;
	
	TransactionSet trans;
	TransactionSetEventHolder eventholder;
	
	enum PositionState {
		Empty, LongContinue, LongReversed, ShortContinue, ShortReversed
	}
	PositionState state;
	
	enum MarkerState {
		WaitForSession, WaitToEnter, GoingUp, GoingDown, Done
	}
	MarkerState stateMarker;
	MarkerState statePoints;
	MarkerState stateSimpleTrading;
	
	int intOrderSize = 100;
	int intMaxScaleIn = 5;
	int intScaleInSize = 1;
	double PredictedPriceJump = 0;
	double hardstop = 0.60;
	double trailingstop = 1.00;
	int PositionRequested = 0;
	int PositionSoFar = 0;
	
	int CurrentMarkerIndex;
	double CurrentMarkerValue;
	int TrailingMarkerIndex;
	double TrailingMarkerValue;
	double TrailingStop;
	double ParabolicAccelerationFactor;
	
	DirectionIndicator di;
	
	DoubleSeries dsSentimentBid01;
	DoubleSeries dsSentimentAsk01;
	DoubleSeries dsSentimentDif01;
	DoubleSeries dsSentimentBid10;
	DoubleSeries dsSentimentAsk10;
	DoubleSeries dsSentimentDif10;
	
	double TriggerPt0 = 0;
	double TriggerPt1 = 0;
	DoubleSeries dsTriggerUp;
	DoubleSeries dsTriggerDn;
	
	DoubleSeries dsOne;
	DoubleSeries dsHalf;
	DoubleSeries dsZero;
	
	DoubleSeries dsStop;

	DateTime dtCurrentDay;
	ATSStrategy strat;
	ATSMetaStrategy metastrat;
	MarketManager mm;
  bool InOptimization;
  Individual individual;
	
	OneUnified.IQFeed.OrderBook ob;

#endregion stratVars

	public atscPivot(): base() {
    RatioNode rn = new RatioNode();
    rn = null;
    agoRatioNode arn = new agoRatioNode();
    arn = null;
    //SimpleNode sn = new SimpleNode();
    //sn = null;
	}
		
	public override void Init()
	{
		pivots = new Pivots( Instrument.Symbol );
		
		FirstSessionTradeFound = false;
		state = PositionState.Empty;
		stateMarker = MarkerState.WaitForSession;
		statePoints = MarkerState.WaitForSession;
		
		stateSimpleTrading = MarkerState.WaitToEnter;

    orders = new TrackOrders();
    ExitSignal = false;

		// Quote information
		dsBid = new DoubleSeries( "Bid" );
		dsBid.Color = Color.Red;
		Draw( dsBid, 0 );
		
		dsAsk = new DoubleSeries( "Ask" );
		dsAsk.Color = Color.Blue;
		Draw( dsAsk, 0 );
		
		latestQuote = null;
		statsSpread = new Stats( "Spread" );
		
		avgQuote = new AccumulateValues( "avgQuote", Color.Cyan, 3 );

		// Trade Information
		dsTrade = new DoubleSeries( "Trade" );
		dsTrade.Color = Color.Green;
		Draw( dsTrade, 0 );
		dsTradeVolume = new DoubleSeries( "Trade Volume" );
		dsTradeVolume.Color = Color.Green;
		dsTradeVolume.DrawStyle = EDrawStyle.Bar;
		Draw( dsTradeVolume, 1 );

		vap = new VolumeAtPrice();
		dsVolumeAtPrice = new DoubleSeries( "VAP" );
		dsVolumeAtPrice.Color = Color.DarkSeaGreen;
		Draw( dsVolumeAtPrice, 0 );
		
		TrailingStop = 0;
		dsStop = new DoubleSeries( "Stop" );
		dsStop.Color = Color.Pink;
		Draw( dsStop, 0 );
		
		dtRunningStart = DateTime.Now;

		dsOne = new DoubleSeries( "One" );
		dsOne.Color = Color.Blue;
		dsHalf = new DoubleSeries( "Half" );
		dsHalf.Color = Color.Green;
		dsZero = new DoubleSeries( "Zero" );
		dsZero.Color = Color.Red;
		
		ob = new OneUnified.IQFeed.OrderBook();
		ob.InsideQuoteChangedEventHandler += new EventHandler(OrderBookQuoteEvent);
		
		dsTriggerUp = new DoubleSeries( "Up Mark" );
		dsTriggerUp.Color = Color.Red;
		dsTriggerUp.DrawStyle = EDrawStyle.Circle;
		dsTriggerUp.DrawWidth = 6;
		Draw( dsTriggerUp, 0 );
		
		dsTriggerDn = new DoubleSeries( "Dn Mark" );
		dsTriggerDn.Color = Color.Blue;
		dsTriggerDn.DrawStyle = EDrawStyle.Circle;
		dsTriggerDn.DrawWidth = 6;
		Draw( dsTriggerDn, 0 );

		//Draw( dsOne, 2 );
		//Draw( dsHalf, 2 );
		//Draw( dsZero, 2 );
		
		Draw( dsOne, 3 );
		Draw( dsHalf, 3 );
		Draw( dsZero, 3 );
		
		Draw( dsOne, 4 );
		Draw( dsHalf, 4 );
		Draw( dsZero, 4 );
		
		rAccum = new AccumulateQuotes[] {
			//new AccumulateQuotes( "Accum 032s",    32, 10000, 1.9, true, Color.DarkCyan ),   //  0.5 minutes
			new AccumulateQuotes( "Accum 096s",    96, 10000, 2.0, true, Color.DarkGray ),   //  1.6 minutes
			new AccumulateQuotes( "Accum 256s",   256, 10000, 2.0, true, Color.Orange ),     //  4.3 minutes
			new AccumulateQuotes( "Accum 768s",   768, 10000, 2.0, true, Color.DarkOrchid ), // 12.8 minutes
			//new AccumulateQuotes( "Accum 2048s", 2048, 10000, 1.9, true, Color.Fuchsia ),    // 34.1 minutes
			//new Accumulation( "Accum 4096s", 4096, 10000, 2, bars, BarWidth, true, Color.Lavender ), // 68.3 minutes
			//new Accumulation( "Accum 1024s", 1024, 10000, 2, bars, BarWidth, true, Color.DarkSalmon ), // 17 minutes
			//new Accumulation( "Accum 512s", 512, 10000, 1.8, bars, BarWidth, true, Color.Orange ), // 8.5 minutes
			//new Accumulation( "Accum 192s", 192, 10000, 1.8, bars, BarWidth, false, Color.Blue ),  // #*  // 3.2 minutes
			//new Accumulation( "Accum 128s", 128, 10000, 2, bars, BarWidth, true, false, Color.DarkCyan ),  // # 2.1 minutes
			//new Accumulation( "Accum 064s",  64, 10000, 2, bars, BarWidth, true, false, Color.DarkGray ),


			}; 
		
		rAccum[0].EnclosingAccumulation = rAccum[0];
		//Draw( rAccum[0].dsSlope, 4 );
		//Draw( rAccum[0].dsSlopeAvg, 4 );
		//Draw( rAccum[0].dsAccelAvg, 5 );
		//Draw( rAccum[0].dsAccel, 5 );
		//Draw( rAccum[0].dsRR, 5 );
		Draw( rAccum[0].dsAvg, 0 );
		Draw( rAccum[0].dsBBUpper, 0 );
		Draw( rAccum[0].dsBBLower, 0 );
			
		//Draw( rAccum[0].dsNormalizedMinMax, 2 );
		Draw( rAccum[0].dsB, 3 );
		Draw( rAccum[0].dsBandwidth, 4 );
		//Draw( rAccum[0].dsER, 9 );
		//Draw( rAccum[0].mfi, 10 );
		//if ( rAccum[0].CalcSlope ) Draw( rAccum[0].dsSD, 7 );
		//for ( int i = rAccum.Length - 1; i >= 1; i-- ) {
		for ( int i = 1; i <= rAccum.Length - 1; i++ ) {
			rAccum[i].EnclosingAccumulation = rAccum[ i - 1 ];
			//Draw( rAccum[i].dsSlope, 4 );
			//Draw( rAccum[i].dsSlopeAvg, 4 );
			//Draw( rAccum[i].dsAccelAvg, 5 );
			//Draw( rAccum[i].dsAccel, 5 );
			//Draw( rAccum[i].dsRR, 5 );
			Draw( rAccum[i].dsAvg, 0 );
			Draw( rAccum[i].dsBBUpper, 0 );
			Draw( rAccum[i].dsBBLower, 0 );
			//Draw( rAccum[i].dsNormalizedMinMax, 2 );
			Draw( rAccum[i].dsB, 3 );
			Draw( rAccum[i].dsBandwidth, 4 );
			//Draw( rAccum[i].dsER, 9 );
			//Draw( rAccum[i].mfi, 10 );
			//if ( rAccum[i].CalcSlope ) Draw( rAccum[i].dsSD, 7 );
		}
		//Draw( dsOne, 4 );
		//Draw( dsHalf, 4 );
		//Draw( dsZero, 4 );
		//Draw( dsOne, 5 );
		//Draw( dsHalf, 5 );
		//Draw( dsZero, 5 );
		
		int ixAccum = 0;
		//rAccum[ixAccum++].bbwMax = 2.5; // 32
		rAccum[ixAccum++].bbwMax = 3.0; // 96
		rAccum[ixAccum++].bbwMax = 4.0; //256
		rAccum[ixAccum++].bbwMax = 6.5; //768

		/*
		Draw( dsZero, 2 );
		dsSentimentAsk01 = new DoubleSeries( "Sent Ask 01" );
		dsSentimentAsk01.Color = Color.Blue;
		Draw( dsSentimentAsk01, 2 );
		dsSentimentDif01 = new DoubleSeries( "Sent Dif 01" );
		dsSentimentDif01.Color = Color.Green;
		Draw( dsSentimentDif01, 2 );
		dsSentimentBid01 = new DoubleSeries( "Sent Bid 01" );
		dsSentimentBid01.Color = Color.Red;
		Draw( dsSentimentBid01, 2 );
		
		Draw( dsZero, 3 );
		dsSentimentAsk10 = new DoubleSeries( "Sent Ask 10" );
		dsSentimentAsk10.Color = Color.Blue;
		Draw( dsSentimentAsk10, 3 );
		dsSentimentDif10 = new DoubleSeries( "Sent Dif 10" );
		dsSentimentDif10.Color = Color.Green;
		Draw( dsSentimentDif10, 3 );
		dsSentimentBid10 = new DoubleSeries( "Sent Bid 10" );
		dsSentimentBid10.Color = Color.Red;
		Draw( dsSentimentBid10, 3 );
		*/
		
		eventholder = new TransactionSetEventHolder();
		eventholder.UpdateSignalStatus += OnTransactionSetExit;
		
		strat = base.Strategy;
		metastrat = strat.ATSMetaStrategy;
		mm = strat.MarketManager;

    InOptimization = false;
    if ("metagp" == metastrat.Name) {
      if (null != metastrat.Optimizer) {
        if (metastrat.Optimizer is GPOptimizer) {
          GPOptimizer gpo = (GPOptimizer) metastrat.Optimizer;
          individual = gpo.curIndividual;
          InOptimization = true;
        }
      }
    }
    

		IExecutionProvider ieprov = strat.ExecutionProvider;
		IMarketDataProvider imdprov = strat.MarketDataProvider;
		
		TimeSpan tsDayStartDelay = new TimeSpan( 0, 3, 0 );
		//TimeSpan tsDayEndWrapup = new TimeSpan( 0, 12, 0 );
		TimeSpan tsDayEndWrapup = new TimeSpan( 0, 3, 0 );
		
		if (MetaStrategyMode.Live == strat.ATSMetaStrategy.MetaStrategyMode ) {
			dtStartingDate = DateTime.Today;
			//string prov = "IQFeed";
			IBarFactory factory = ProviderManager.MarketDataProviders[imdprov.Id].BarFactory;
			factory.Items.Clear();
			factory.Items.Add(new BarFactoryItem(BarType.Time, BarWidth, true ));
			factory.Enabled = true;
			
			instrument.RequestMarketData(
				ProviderManager.MarketDataProviders[imdprov.Id],MarketDataType.Quote);
			instrument.RequestMarketData(
				ProviderManager.MarketDataProviders[imdprov.Id],MarketDataType.Trade);

			SessionBegin = new TimeSpan(10, 30, 00);
			SessionEnd   = new TimeSpan(17, 00, 00);
		}
		else {
			dtStartingDate = strat.MetaStrategyBase.SimulationManager.EntryDate;
			dtEndingDate   = strat.MetaStrategyBase.SimulationManager.ExitDate;
			//Console.WriteLine( "{0} Simulation Range {1} to {2}", Instrument.Symbol, dtStartingDate, dtEndingDate );
			IBarFactory factory = ProviderManager.MarketDataProviders[1].BarFactory;
			factory.Items.Clear();
			factory.Enabled = true;
			factory.Items.Add(new BarFactoryItem(BarType.Time, BarWidth, true ));
			
			SessionBegin = dtStartingDate.TimeOfDay;
			SessionEnd   = dtEndingDate.TimeOfDay;

			// these lines are located in SimulationManager Component
			//SendMarketDataRequest("Quote");
			//SendMarketDataRequest("Trade");

		}
		TradingTimeBegin = SessionBegin + tsDayStartDelay;
		TradingTimeEnd   = SessionEnd   - tsDayEndWrapup;
		
		//Console.WriteLine( "{0} Trading Range {1} to {2}", Instrument.Symbol, TradingTimeBegin, TradingTimeEnd  );
		dtCurrentDay = dtStartingDate.Date;
	}

	public override void OnBarOpen( Bar bar ) {

		//Console.WriteLine( "{0} onbaropen {1}", Instrument.Symbol, bar );
		if ( ( bar.DateTime.TimeOfDay >= SessionBegin )
		  && ( bar.DateTime.TimeOfDay <  SessionEnd   ) ) {

			if ( !FirstSessionTradeFound && bar.Size == BarWidth ) {
				FirstSessionTradeFound = true;
				pivots.SetOpeningTrade( bar.Open );
				pivots.Draw( this );
				double d = Math.Max( 0.08, pivots.AverageDelta / 4 );
				// **Console.WriteLine( "{0} di interval {1:0.00}", Instrument.Symbol, d );
				di = new DirectionIndicator( 
					//Math.Max( 0.07, ( pivots.sixmonposcrossover - pivots.sixmonnegcrossover ) / 4 ) );
				    //d );
					0.06 );
				Draw( di.dsPattern, 0 );
				//Draw( di.dsSellDecisionPoint, 0 );
				//Draw( di.dsBuyDecisionPoint, 0 );
			}
		}
	}
	
	public override void OnBar(Bar bar) {
		if ( ( bar.DateTime.TimeOfDay >= SessionBegin )
		  && ( bar.DateTime.TimeOfDay <  SessionEnd   ) ) {
  			
			//bars.Add( bar );
			
			if ( bar.Size == BarWidth ) {
				pivots.OnBar( bar );
			
				if ( vap.LargestVolume > 0 ) {
					dsVolumeAtPrice.Add( bar.DateTime, vap.PriceAtLargestVolume );
				}
				if ( TrailingStop > 0 ) {
					dsStop.Add( bar.DateTime, TrailingStop );
				}
			}
			
		
			dsOne.Add( bar.DateTime, 1.0 );
			dsHalf.Add( bar.DateTime, 0.5 );
			dsZero.Add( bar.DateTime, 0.0 );
			
		}
	}
	
	public override void OnTrade( Trade trade ) {
		
		//string s = trade.DateTime.ToString("HH:mm:ss.fff");
		//Console.WriteLine( "onTrade {0} {1:#.00} {2:#.00}", s, trade.Price, trade.Size );
		
		if ( ( trade.DateTime.TimeOfDay >= SessionBegin )
		  && ( trade.DateTime.TimeOfDay <  SessionEnd   ) ) {
			CanTrade = true;
			dsTrade.Add( trade.DateTime, trade.Price );
			dsTradeVolume.Add( trade.DateTime, trade.Size );
			vap.Add( trade );
		}
	}
	
	public override void OnQuote( Quote quote ) {

		//string s = quote.DateTime.ToString("HH:mm:ss.fff");
		//Console.WriteLine( "onQuote {0} b/a {1:@#.00}/{2:#.00}", 
		//	s, quote.Bid, quote.Ask );
		
		//Console.WriteLine( "2 Quote {0} {1} {2}", quote.DateTime, quote.Bid, quote.Ask );
		
		double qHi = Math.Max( quote.Bid, quote.Ask );
		double qLo = Math.Min( quote.Bid, quote.Ask );
		double val = Math.Round(( qHi + qLo ) / 2.0, 2);
		double spread = qHi - qLo;

		if ( 0.20 > ( spread ) ) {
			dsBid.Add( quote.DateTime, quote.Bid );
			dsAsk.Add( quote.DateTime, quote.Ask );
			
			
			//dsBidVolume.Add( quote.DateTime, -quote.BidSize );
			//dsAskVolume.Add( quote.DateTime,  quote.AskSize );
		}
			
		//		if ( null != latestQuote ) {
		//			double lqHi = Math.Max( latestQuote.Bid, latestQuote.Ask );
		//			double lqLo = Math.Min( latestQuote.Bid, latestQuote.Ask );
		//		}
		latestQuote = quote;
		statsSpread.Add( qHi - qLo );
		
		//		if ( null != latestRoundTrip ) {
		//			latestRoundTrip.UpdateQuote( quote );
		//		}
		
		eventholder.UpdateQuote( this, quote );

    ExitSignal = false;
    //BuySignal = false;
    //SellSignal = false;
    LongEnter = false;
    LongExit = false;
    ShortEnter = false;
    ShortExit = false;

		//gpstrategy.EmitSignalStage1(this);  // this clears signals so needs to come first
		
		if ( quote.DateTime.TimeOfDay >= TradingTimeEnd ) {
			ExitSignal = true;
			stateMarker = MarkerState.Done;
		}
		
		if ( quote.DateTime.TimeOfDay < TradingTimeBegin) {
			ExitSignal = true;
		}
			
		//========================
		
		
		
		if ( FirstSessionTradeFound ) {
			
			if ( ( spread ) <= 0.20 ) {
				di.Add( quote );
			}

			bool bState = false;
			bool bStateUp = false;
			bool bStateDn = false;
			if ( ( spread ) <= 0.20 ) {
				switch ( statePoints ) {
					case MarkerState.WaitForSession:
						TriggerPt0 = val;
						TriggerPt1 = val;
						statePoints = MarkerState.WaitToEnter;
						break;
					case MarkerState.WaitToEnter:
						if ( val == TriggerPt0 ) {
						}
						else {
							if ( val > TriggerPt0 ) {
								statePoints = MarkerState.GoingUp;
							}
							if ( val < TriggerPt0 ) {
								statePoints = MarkerState.GoingDown;
							}
							TriggerPt0 = val;
						}
						break;
					case MarkerState.GoingUp:
						//if ( quote.Ask > TriggerPt0 ) {
            if (quote.Bid > TriggerPt0) {
							TriggerPt1 = TriggerPt0;
							//TriggerPt0 = quote.Ask;
              TriggerPt0 = quote.Bid;
							//dsTriggerUp.Add( quote.DateTime, quote.Ask );
              dsTriggerUp.Add(quote.DateTime, quote.Bid);
							bState = true;
							bStateUp = true;
						}
						else {
							if ( quote.Ask < TriggerPt1 ) {
								TriggerPt0 = quote.Ask;
								dsTriggerDn.Add( quote.DateTime, quote.Ask );
								statePoints = MarkerState.GoingDown;
								bState = true;
								bStateDn = true;
							}
						}
						break;
					case MarkerState.GoingDown:
						//if ( quote.Bid < TriggerPt0 ) {
            if (quote.Ask < TriggerPt0) {
							TriggerPt1 = TriggerPt0;
							//TriggerPt0 = quote.Bid;
              TriggerPt0 = quote.Ask;
							//dsTriggerDn.Add( quote.DateTime, quote.Bid );
              dsTriggerDn.Add(quote.DateTime, quote.Ask);
							bState = true;
							bStateDn = true;
						}
						else {
							if ( quote.Bid > TriggerPt1 ) {
								TriggerPt0 = quote.Bid;
								dsTriggerUp.Add( quote.DateTime, quote.Bid );
								statePoints = MarkerState.GoingUp;
								bState = true;
								bStateUp = true;
							}
						}
						break;
				}
			}

			int cntOrders = 0;
			//ECross cross;
			
			//avgQuote.Add( quote.DateTime, val );
			//avgQuote.CheckWindow();
			//avgQuote.CalcStats();
			
			//double t2 = avgQuote.dsAvg.Last;
			//double t2 = val;
			//if ( quote.DateTime > new DateTime(2006, 12, 14, 12, 17, 30 ) 
			//& quote.DateTime < new DateTime( 2006, 12, 14, 12, 18, 00 ) ) {
			//	Console.WriteLine( "t2 {0} {1}", quote.DateTime, t2 );
			//}
			//if ( t2 > 0 ) {
			
			foreach ( AccumulateQuotes accum in rAccum ) {
				//accum.Add( quote.DateTime, val, avgQuoteSpread );
				accum.Add( quote );
				//accum.Add( quote.DateTime, val );
				
				//accum.Add( quote.DateTime, t2 );
				
				if ( false && accum.CalcTrade ) {
					
					

					
					if ( accum.dsBBUpper.Last - accum.dsBBLower.Last >= 0.06 ) {
						// need a trading range for profit
						if ( bStateDn && val > accum.dsAvg.Last ) {
							//if ( TriggerPt0 > accum.dsBBUpper.Last ) {
							//cntOrders--;
							//}
						}
						if ( bStateUp && val < accum.dsAvg.Last ) {
							//if ( TriggerPt0 < accum.dsBBLower.Last ) {
							//cntOrders++;
							//}
						}
					}

			
				}
			}
			//}

      if (bState) {
        if (InOptimization) {
          LongEnter = individual.LongEnter.EvaluateBool(this);
          LongExit = individual.LongExit.EvaluateBool(this);
          ShortEnter = individual.ShortEnter.EvaluateBool(this);
          ShortExit = individual.ShortExit.EvaluateBool(this);
          //BuySignal = individual.LongSignal.EvaluateBool(this);
          //SellSignal = individual.ShortSignal.EvaluateBool(this);
        }
        else {
//id 5219 raw 101583.00 adj 1.00000 norm .02764
  //LongEnter = (rAccum[1].minmax.MinFalling||rAccum[0].minmax.MaxRising);
  //ShortEnter = ((rAccum[0].dsB.Last + rAccum[1].dsBandwidth.Ago(19)) >= rAccum[1].dsNormalizedMinMax.Last);
          //BuySignal = (rAccum[1].dsNormalizedMinMax.Last - rAccum[1].dsB.Last) >= 0.5798;
          //SellSignal = (rAccum[0].minmax.MaxFalling
          //  && (rAccum[0].minmax.MaxFalling && rAccum[0].minmax.MinRising)) && rAccum[1].minmax.MaxFalling;
  /*
   * id 3055 raw 101378.00 adj .00485 norm .00220
Long 4 root=(rAccum[1].minmax.MinFalling||rAccum[0].minmax.MaxRising)
Shrt 2 root=!((rAccum[1].dsNormalizedMinMax.Last+0.485)>=(rAccum[0].dsB.Ago(31)-rAccum[1].dsB.Last))
   * 
   * id 2898 raw 100926.00 adj .00152 norm .00069
Long 3 root=((rAccum[0].dsB.Last+rAccum[0].dsB.Last)>=rAccum[1].dsNormalizedMinMax.Last)
Shrt 3 root=((rAccum[0].dsB.Last+rAccum[1].dsBandwidth.Ago(19))>=rAccum[1].dsNormalizedMinMax.Last)
   */
}
        if (LongEnter) {
          hardstop = val - 02.95;
        }
        if (ShortEnter) {
          hardstop = val + 02.95;
        }
        GenerateOrder(this);
      }
			
			

      /*
			switch ( stateSimpleTrading ) {
				case MarkerState.WaitToEnter:
					if ( rAccum[2].minmax.Min > pivots.sixmonposcrossover ) {
						stateSimpleTrading = MarkerState.GoingUp;
						cntOrders++;
						// **Console.WriteLine( "buy {0}", quote.DateTime );
					}
					if ( rAccum[2].minmax.Max < pivots.sixmonnegcrossover ) {
						stateSimpleTrading = MarkerState.GoingDown;
						cntOrders--;
						// **Console.WriteLine( "sell {0}", quote.DateTime );
					}
					break;
				case MarkerState.GoingUp:
					if ( rAccum[2].minmax.Max < pivots.sixmonposcrossover ) {
						stateSimpleTrading = MarkerState.WaitToEnter;  // dangerous, need count here
						cntOrders--;
					}
				break;
				case MarkerState.GoingDown:
					if ( rAccum[2].minmax.Min > pivots.sixmonnegcrossover ) {
						stateSimpleTrading = MarkerState.WaitToEnter;  // dangerous, need count here
						cntOrders++;
					}
					break;
				case MarkerState.Done:
				break;
			}
       */

			//if ( bStateDn ) cntOrders--;
			//if ( bStateUp ) cntOrders++;
			
      /*
			while ( 0 != cntOrders ) {
				if ( cntOrders > 0 ) {
					hardstop = val - 02.95;
					BuySignal = true;
					//GenerateOrder( this, gpstrategy );
					//cntOrders--;
					cntOrders=0;
				}
				if ( cntOrders < 0 ) {
					hardstop = val + 02.95;
					SellSignal = true; 
					//GenerateOrder( this, gpstrategy );
					//cntOrders++;
					cntOrders=0;
				}
			}
       */

			
		
		}	
		
		//GenerateOrder( this );
			
			
		//========================
		

		//if ( BuySignal || SellSignal ) {
			//				dtLastDirectionSignal = quote.DateTime;
		//}
		//else {
			//				if ( quote.DateTime > ( dtLastDirectionSignal + new TimeSpan(0,0,0,0,intExitWait) ) ) {
			//					gpstrategy.ExitSignal = true;
			//				}
		//}
	
	}

	private void OrderBookQuoteEvent( object o, EventArgs args ) {
		
		if ( ob.slAsk.Count > 0 && ob.slBid.Count > 0 ) {
			MarketMakerBidAsk mmbaBid = (MarketMakerBidAsk)ob.slBid.GetByIndex( 0 );
			MarketMakerBidAsk mmbaAsk = (MarketMakerBidAsk)ob.slAsk.GetByIndex( 0 );
			//quote = new Quote(Clock.Now, mmbaBid.Bid, mmbaBid.BidSize, mmbaAsk.Ask, mmbaAsk.AskSize );
			
			int ixBid = ob.slPrice.IndexOfKey( mmbaBid.Bid );
			int ixAsk = ob.slPrice.IndexOfKey( mmbaAsk.Ask );
			
			int bidsize = (int) ob.slPrice.GetByIndex( ixBid );
			int asksize = (int) ob.slPrice.GetByIndex( ixAsk );
			dsSentimentAsk01.Add( Clock.Now, asksize );
			dsSentimentDif01.Add( Clock.Now, asksize - bidsize );
			dsSentimentBid01.Add( Clock.Now, -bidsize );
			
			int bidttl = 0;
			int askttl = 0;
			
			double limit;
			double price;
			int size;
			
			limit = mmbaBid.Bid - 0.10;
			while ( true ) {
				price = (double) ob.slPrice.GetKey( ixBid );
				if ( price > limit ) {
					size = (int) ob.slPrice.GetByIndex( ixBid );
					bidttl += size;
					ixBid--;
				}
				else break;
			}
			limit = mmbaAsk.Ask + 0.10;
			while ( true ) {
				price = (double) ob.slPrice.GetKey( ixAsk );
				if ( price < limit ) {
					size = (int) ob.slPrice.GetByIndex( ixAsk );
					askttl += size;
					ixAsk++;
				}
				else break;
			}

			dsSentimentAsk10.Add( Clock.Now, askttl );
			dsSentimentDif10.Add( Clock.Now, askttl - bidttl );
			dsSentimentBid10.Add( Clock.Now, -bidttl );
		}

	}
	
	public override void OnMarketDepth( MarketDepth depth ) {
		ob.Update( depth );
	}
	
	public override void OnStrategyStop() {
		//Console.WriteLine( "OnStrategyStop");
		
		try {

			eventholder.StrategyStop( this, EventArgs.Empty );
			eventholder.UpdateSignalStatus -= OnTransactionSetExit;
		
      /*
			//Console.WriteLine( "{0} OSS 1", Instrument.Symbol );
			if ( !RoundTrip.bTotalEmitted ) {
				Console.WriteLine( "Profit/Loss Theo {0:0.00} Avg {1:0.00} Actual {2:0.00}", 
					RoundTrip.ttlMaxPL, RoundTrip.ttlMaxPL/RoundTrip.ttlCntPL, RoundTrip.ttlActPL );
				RoundTrip.bTotalEmitted = true;
				RoundTrip.Reset();
			}
		*/
			//Console.WriteLine( "{0} OSS 2", Instrument.Symbol );
			if ( null != di ) {
				//di.Report();
			}
			//Console.WriteLine( "{0} OSS 3", Instrument.Symbol );
			statsSpread.Report();
			//Console.WriteLine( "{0} gpis.orders.count {1}", Instrument.Symbol, orders.Count );
		
			//Console.WriteLine( "Start {0} Duration {1} {2}", dtRunningStart, DateTime.Now - dtRunningStart, Instrument.Symbol );

		}
		catch ( Exception e ) {
			Console.WriteLine( "OSS Exception: " + e );
		}
	}
	
	private void OnTransactionSetExit( object source, bool Exited ) {
		if ( Exited ) {
			TransactionSet trans = (TransactionSet) source;
			if ( this.trans == trans ) {
				//Console.WriteLine( "onTransactionSetExit" );
				state = PositionState.Empty;
			}
		}
	}

  public void GenerateOrder( object source ) {
    try {

      //Console.WriteLine("gps state {0} {1} {2} {3} {4}", 
      //	state, gpstrategy.ExitSignal, gpstrategy.BuySignal, gpstrategy.SellSignal, Instrument.Symbol);
      if (ExitSignal) {
        // exit everything
        while (0 != orders.Count) {
          //for ( int i = gpis.orders.Count; i > 0; i-- ) {
          //Console.WriteLine( "Exit Orders" );
          TransactionSet trans = orders.Remove();
          trans.UpdateSignal(TransactionSet.ESignal.Exit);
        }
      }
      else {
        if (
          // *** do we need to make these mutually exclusive?
               ((LongEnter ^ LongExit) || (ShortEnter ^ ShortExit))
             && !(LongEnter && ShortEnter)
          ) {
          // choose how to process the order
          // Exit positions that are to be reversed
          if ((orders.Count > 0 && ShortEnter && (ShortEnter ^ ShortExit)) 
            || (orders.Count < 0 && LongEnter && (LongEnter ^ LongExit))) {
            // Remove transaction and close out 
            while (0 != orders.Count) {
              TransactionSet trans = orders.Remove();
              trans.UpdateSignal(TransactionSet.ESignal.Exit);
            }
          }
          if ((0 == orders.Count)
            //|| ( orders.Count > 0 && BuySignal ) || ( orders.Count < 0 && SellSignal ) 
            ) {
            // Start transaction with an order
            if (Math.Abs(orders.Count) < intMaxScaleIn) {
              if (LongEnter) {
                TransactionSet trans = new TransactionSet(this);
                TrackOrdersLink OrderLink = new TrackOrdersLink(orders, orders.AddBuy(trans));
                trans.Create(TransactionSet.ESignal.Long, OrderLink,
                  intOrderSize, latestQuote, PredictedPriceJump,
                  hardstop, trailingstop, eventholder);
              }
              if (ShortEnter) {
                TransactionSet trans = new TransactionSet(this);
                TrackOrdersLink OrderLink = new TrackOrdersLink(orders, orders.AddSell(trans));
                trans.Create(TransactionSet.ESignal.Short, OrderLink,
                  intOrderSize, latestQuote, PredictedPriceJump,
                  hardstop, trailingstop, eventholder);
              }
            }
          }
          // Exit the held positions
          if ((orders.Count > 0 && LongExit && (LongEnter ^ LongExit))
            || (orders.Count < 0 && ShortExit && (ShortEnter ^ ShortExit))) {
            // Remove transaction and close out 
            TransactionSet trans = orders.Remove();
            trans.UpdateSignal(TransactionSet.ESignal.Exit);
          }
          //Console.WriteLine( "#trans={0} b:{1} s:{2} ", gpis.orders.Count, gpstrategy.BuySignal, gpstrategy.SellSignal );
        }
      }

    }
    catch (Exception e) {
      Console.WriteLine("problems {0}" + e);
    }
  }

}

#region RatioNode

public class RatioNode : DoubleNode {
  // base
  // output one double
  // input one double
  // typical range is 0..1, but can exceed either end

  protected int Index;

  static RatioNode() {
    RatioBandwidth rba = new RatioBandwidth();
    rba = null;
    RatioB rb = new RatioB();
    rb = null;
    RatioVal rv = new RatioVal();
    rv = null;
    //RatioMinMax rmm = new RatioMinMax();
    //rmm = null;
  }

  public RatioNode() {
    Terminal = true;
    cntNodes = 0;
    Index = random.Next(0, 2);  // number of elements in rAccum
  }

  public RatioNode( int Index ) {
    Terminal = true;
    cntNodes = 0;
    this.Index = Index; 
  }

  protected override void CopyValuesTo( Node node ) {
    // copy this.values to replicated copy
    RatioNode rn = node as RatioNode;
    rn.Index = this.Index;
  }

  public override double EvaluateDouble( object o ) {
    throw new NotSupportedException("Can not call EvaluateDouble on void base (RatioNode).");
  }

}

public class RatioVal : RatioNode {
  // output one double
  // no input

  double val;

  static RatioVal() {
    alDoubleNodes.Add(typeof(RatioVal));
  }

  public RatioVal() {
    val = Math.Round( random.NextDouble(), 4 ); // between 0 and 1
  }

  public RatioVal( double val ) {
    // will this actually get hit?
    this.val = val;
  }

  public override string ToString() {
    return val.ToString();
    //return val.ToString("0.0000");
  }

  protected override void CopyValuesTo( Node node ) {
    // copy this.values to replicated copy
    RatioVal rv = node as RatioVal;
    rv.val = this.val;
  }
  
  public override double EvaluateDouble( object o ) {
    return val;
  }
}

/*
public class RatioMinMax : RatioNode {
  // output one double
  // no input

  static RatioMinMax() {
    alDoubleNodes.Add(typeof(RatioMinMax));
  }

  public override string ToString() {
    return "rAccum[" + Index.ToString() + "].dsNormalizedMinMax.Last";
  }

  public override double EvaluateDouble( object o ) {
    if ((o as atscPivot).rAccum[Index].dsNormalizedMinMax.Count > 0) {
      return (o as atscPivot).rAccum[Index].dsNormalizedMinMax.Last;
    }
    else return (o as atscPivot).Default;
  }
}
*/

public class RatioB : RatioNode {
  // output one double
  // no input

  static RatioB() {
    alDoubleNodes.Add(typeof(RatioB));
  }

  public override string ToString() {
    return "rAccum[" + Index.ToString() + "].dsB.Last";
  }

  public override double EvaluateDouble( object o ) {
    if ((o as atscPivot).rAccum[Index].dsB.Count > 0) {
      return (o as atscPivot).rAccum[Index].dsB.Last;
    }
    else return (o as atscPivot).Default;
  }
}

public class RatioBandwidth : RatioNode {
  // output one double
  // no input

  static RatioBandwidth() {
    alDoubleNodes.Add(typeof(RatioBandwidth));
  }

  public override string ToString() {
    return "rAccum[" + Index.ToString() + "].dsBandwidth.Last";
  }

  public override double EvaluateDouble( object o ) {
    if ((o as atscPivot).rAccum[Index].dsBandwidth.Count > 0) {
      return (o as atscPivot).rAccum[Index].dsBandwidth.Last;
    }
    else return (o as atscPivot).Default;
  }
}

#endregion RatioNode

#region agoRatioNode

public class agoRatioNode : DoubleNode {
  // base
  // output one double
  // input one double
  // typical range is 0..1, but can exceed either end

  protected int Index;
  protected int Ago;

  static agoRatioNode() {
    agoRatioBandwidth rba = new agoRatioBandwidth();
    rba = null;
    agoRatioB rb = new agoRatioB();
    rb = null;
    //agoRatioMinMax rmm = new agoRatioMinMax();
    //rmm = null;
  }

  public agoRatioNode() {
    Terminal = true;
    cntNodes = 0;
    Index = random.Next(0, 2);  // number of elements in rAccum
    Ago = random.Next(1, 50);
  }

  public agoRatioNode( int Index, int Ago ) {
    Terminal = true;
    cntNodes = 0;
    this.Index = Index;
    this.Ago = Ago;
  }

  protected override void CopyValuesTo( Node node ) {
    // copy this.values to replicated copy
    agoRatioNode rn = node as agoRatioNode;
    rn.Index = this.Index;
    rn.Ago = this.Ago;
  }

  public override double EvaluateDouble( object o ) {
    throw new NotSupportedException("Can not call EvaluateDouble on void base (RatioNode).");
  }

}

/*
public class agoRatioMinMax : agoRatioNode {
  // output one double
  // no input

  static agoRatioMinMax() {
    alDoubleNodes.Add(typeof(agoRatioMinMax));
  }

  public override string ToString() {
    return "rAccum[" + Index.ToString() + "].dsNormalizedMinMax.Ago(" + Ago.ToString() + ")";
  }

  public override double EvaluateDouble( object o ) {
    if ((o as atscPivot).rAccum[Index].dsNormalizedMinMax.Count > Ago) {
      return (o as atscPivot).rAccum[Index].dsNormalizedMinMax.Ago(Ago);
    }
    else return (o as atscPivot).Default;
  }
}
 */

public class agoRatioB : agoRatioNode {
  // output one double
  // no input

  static agoRatioB() {
    alDoubleNodes.Add(typeof(agoRatioB));
  }

  public override string ToString() {
    return "rAccum[" + Index.ToString() + "].dsB.Ago(" + Ago.ToString() + ")";
  }

  public override double EvaluateDouble( object o ) {
    if ((o as atscPivot).rAccum[Index].dsB.Count > Ago) {
      return (o as atscPivot).rAccum[Index].dsB.Ago(Ago);
    }
    else return (o as atscPivot).Default;
  }
}

public class agoRatioBandwidth : agoRatioNode {
  // output one double
  // no input

  static agoRatioBandwidth() {
    alDoubleNodes.Add(typeof(agoRatioBandwidth));
  }

  public override string ToString() {
    return "rAccum[" + Index.ToString() + "].dsBandwidth.Ago(" + Ago.ToString() + ")";
  }

  public override double EvaluateDouble( object o ) {
    if ((o as atscPivot).rAccum[Index].dsBandwidth.Count > Ago) {
      return (o as atscPivot).rAccum[Index].dsBandwidth.Ago(Ago);
    }
    else return (o as atscPivot).Default;
  }
}

#endregion agoRatioNode

/*
#region SimpleNode

public class SimpleNode : BoolNode {
  // base
  // output one bool
  // input one bool

  protected int Index;

  static SimpleNode() {
    SimpleMaxRising axr = new SimpleMaxRising();
    axr = null;
    SimpleMaxFalling axf = new SimpleMaxFalling();
    axf = null;
    SimpleMinRising inr = new SimpleMinRising();
    inr = null;
    SimpleMinFalling inf = new SimpleMinFalling();
    inf = null;
  }

  public SimpleNode() {
    Terminal = true;
    cntNodes = 0;
    Index = random.Next(0, 2);  // number of elements in rAccum
  }

  public SimpleNode( int Index ) {
    Terminal = true;
    cntNodes = 0;
    this.Index = Index; 
  }

  protected override void CopyValuesTo( Node node ) {
    // copy this.values to replicated copy
    SimpleNode sn = node as SimpleNode;
    sn.Index = this.Index;
  }

  public override double EvaluateDouble( object o ) {
    throw new NotSupportedException("Can not call EvaluateDouble on void base (SimpleNode).");
  }

}

public class SimpleMaxRising : SimpleNode {
  // output one bool
  // no input

  static SimpleMaxRising() {
    alBoolNodes.Add(typeof(SimpleMaxRising));
  }

  public override string ToString() {
    return "rAccum[" + Index.ToString() + "].minmax.MaxRising";
  }

  public override bool EvaluateBool( object o ) {
    return (o as atscPivot).rAccum[Index].minmax.MaxRising;
  }
}

public class SimpleMaxFalling : SimpleNode {
  // output one bool
  // no input

  static SimpleMaxFalling() {
    alBoolNodes.Add(typeof(SimpleMaxFalling));
  }

  public override string ToString() {
    return "rAccum[" + Index.ToString() + "].minmax.MaxFalling";
  }

  public override bool EvaluateBool( object o ) {
    return (o as atscPivot).rAccum[Index].minmax.MaxFalling;
  }
}

public class SimpleMinRising : SimpleNode {
  // output one bool
  // no input

  static SimpleMinRising() {
    alBoolNodes.Add(typeof(SimpleMinRising));
  }

  public override string ToString() {
    return "rAccum[" + Index.ToString() + "].minmax.MinRising";
  }

  public override bool EvaluateBool( object o ) {
    return (o as atscPivot).rAccum[Index].minmax.MinRising;
  }
}

public class SimpleMinFalling : SimpleNode {
  // output one bool
  // no input

  static SimpleMinFalling() {
    alBoolNodes.Add(typeof(SimpleMinFalling));
  }

  public override string ToString() {
    return "rAccum[" + Index.ToString() + "].minmax.MinFalling";
  }

  public override bool EvaluateBool( object o ) {
    return (o as atscPivot).rAccum[Index].minmax.MinFalling;
  }
}

#endregion SimpleNode

*/