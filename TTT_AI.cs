using Godot;
using Godot.Collections;
using System.Linq;
using System.Threading;

public partial class TTT_AI : Node
{
	public Godot.Collections.Array TableData = new Godot.Collections.Array();
	public string CacheFolder = "user://Cache/";
	public Godot.Collections.Array History = new Godot.Collections.Array();
	private bool AutoStartCaching = false;
	private float totalShares = 0.0F;
	private float successShares = 0.0F;
	private float duplicateShares = 0.0F;
	int ExistingCacheCount = 0;
	public Dictionary ThreadData = new();
	bool ThreadOpen = true;
	DirAccess ExistingCacheFolder;
	[Signal]
	public delegate string SetTableEventHandler(Dictionary newTable);

	private void RegisterTable()
	{
		for (int x=0;x<3;x++)
		{
			for (int y=0;y<3;y++)
			{
				TableData.Add(x.ToString() + "," + y.ToString());
			}
		}
	}
	private void LogOutData()
	{
		GD.PrintRich("["+Time.GetTimeStringFromSystem()+"]Share Statistics: \nTotal Shares: "+totalShares.ToString()+"\n[color=green]Successful Shares: [/color]"+successShares.ToString()+" ("+ ExistingCacheCount.ToString() + " Cached before)\n[color=red]Duplicate Shares: [/color]"+duplicateShares.ToString()+ "\n[color=yellow]Share Fail Rate: [/color]" + (int)((duplicateShares/totalShares)*100)+"%\n");
		foreach (var thread in ThreadData.Keys)
		{
			int threadPoint = (int)ThreadData[thread];
			GD.PrintRich("[color=orange]Thread #" + thread.AsString() + ": [/color]" + threadPoint.ToString());
		}
		GD.Print("\n");
		GetTree().CreateTimer(10).Timeout += () => LogOutData();
	}
	public override void _Ready()
	{
		DirAccess ExistingCacheFolder = DirAccess.Open(CacheFolder);
		ExistingCacheCount = ExistingCacheFolder.GetFiles().Length;
		int ThreadCount = OS.GetProcessorCount();
		RegisterTable();
		if (OS.HasFeature("trainer")) { AutoStartCaching = true; }
		if (AutoStartCaching)
		{
			ThreadData.Add("0", 0);
			for (int i = 1; i < ThreadCount; i++)
			{
				GodotThread newThread = new();
				Callable threadProcess = Callable.From(() => _Thread_Process(newThread));
				newThread.SetMeta("threadid", i);
				ThreadData.Add(i.ToString(), 0);
				newThread.Start(threadProcess, GodotThread.Priority.High);
			}
		}
		ProcessPriority = 3;
		GD.Randomize();
		GetTree().CreateTimer(10).Timeout += () => LogOutData();
	}
	public void _Thread_Process(GodotThread thread)
	{
		while (ThreadOpen)
		{
			GenerateCache((int)thread.GetMeta("threadid"));
		}
	}
	public override void _Process(double delta)
	{
		if (AutoStartCaching) { GenerateCache(0); }
	}
	public override void _ExitTree()
	{
		ThreadOpen = false;
	}
	public string ThirtyHash(string input)
	{
		return input;
	}
	public int GenerateCache(int threadId)
	{
		Godot.Collections.Array TempHistoryB1 = new Godot.Collections.Array();
		Godot.Collections.Array TempHistoryB2 = new Godot.Collections.Array();
		Godot.Collections.Array TempHistoryALL = new Godot.Collections.Array();
		Godot.Collections.Array Slots = new Godot.Collections.Array();
		Dictionary SlotsPlayed = new Dictionary();
		foreach (var item in TableData) { Slots.Add(item); }
		if (!DirAccess.DirExistsAbsolute(CacheFolder)) { DirAccess.MakeDirAbsolute(CacheFolder); }
		bool gamePlaying = true;
		string winner = "N";
		string winline = "";
		while (gamePlaying)
		{
			string data = "X";
			for (int i = 0; i<2; i++)
			{
				// Check if game has ended \\
				for (int x = 0; x<3; x++)
				{
					int line = 0;
					for (int y = 0; y<3; y++)
					{
						if (SlotsPlayed.ContainsKey(x.ToString() + "," + y.ToString()))
						{
							if ((string)(SlotsPlayed[x.ToString() + "," + y.ToString()]) == data)
							{
								line++;
							}
						}
					}
					if (line == 3)
					{
						winline = "Y" + x;
						winner = data;
						gamePlaying = false;
					}
				}
				for (int y = 0; y<3; y++)
				{
					int line = 0;
					for (int x = 0; x<3; x++)
					{
						if (SlotsPlayed.ContainsKey(x.ToString() + "," + y.ToString()))
						{
							if ((string)(SlotsPlayed[x.ToString() + "," + y.ToString()]) == data)
							{
								line++;
							}
						}
					}
					if (line == 3)
					{
						winline = "X" + y;
						winner = data;
						gamePlaying = false;
					}
				}
				(int, int) checkX1 = (0, 0);
				int lineX1 = 0;
				for (int x = 0; x < 3; x++)
				{
					if (SlotsPlayed.ContainsKey(checkX1.Item1.ToString() + "," + checkX1.Item2.ToString()))
					{
						if ((string)(SlotsPlayed[checkX1.Item1.ToString() + "," + checkX1.Item2.ToString()]) == data)
						{
							lineX1++;
						}
					}
					checkX1.Item1 += 1;
					checkX1.Item2 += 1;
				}
				if (lineX1 == 3)
				{
					winline = "Diag1";
					winner = data;
					gamePlaying = false;
				}
				(int, int) checkX2 = (2, 0);
				int lineX2 = 0;
				for (int x = 0; x < 3; x++)
				{
					if (SlotsPlayed.ContainsKey(checkX2.Item1.ToString() + "," + checkX2.Item2.ToString()))
					{
						if ((string)(SlotsPlayed[checkX2.Item1.ToString() + "," + checkX2.Item2.ToString()]) == data)
						{
							lineX2++;
						}
					}
					checkX2.Item1 -= 1;
					checkX2.Item2 += 1;
				}
				if (lineX2 == 3)
				{
					winline = "Diag2";
					winner = data;
					gamePlaying = false;
				}
				if (!gamePlaying) { continue; }
				// Play game if not ended \\
				if (Slots.Count > 0)
				{
					string targetSlot = (string)Slots.PickRandom();
					Slots.Remove(targetSlot);
					SlotsPlayed.Add(targetSlot, data);
					if (data == "X") { TempHistoryB1.Add(targetSlot); }
					else if (data == "O") { TempHistoryB2.Add(targetSlot); }
					TempHistoryALL.Add(targetSlot);
				} else
				{
					gamePlaying = false;
				}
				data = "O";
			}
		}
		int XWinStat = 0;
		int OWinStat = 0;
		bool winnerExists = false;
		if (winner == "X")
		{
			XWinStat = 1;
			OWinStat = -1;
			winnerExists = true;
		}
		else if (winner == "O")
		{
			XWinStat = -1;
			OWinStat = 1;
			winnerExists = true;
		}
		string StrHistoryAll = TempHistoryALL.ToString();
		foreach (char c in System.IO.Path.GetInvalidFileNameChars())
		{
			StrHistoryAll = StrHistoryAll.Replace(c, ' ');
		}
		int returnInt = 0;
		if (!FileAccess.FileExists(CacheFolder + "X " + StrHistoryAll + ".bin"))
		{
			using var CacheFilesX = FileAccess.Open(CacheFolder + "X "+ StrHistoryAll +".bin", FileAccess.ModeFlags.Write);
			if (CacheFilesX == null) { GD.PrintRich(Time.GetTimeStringFromSystem() + " [X] Cache Error: [color=red]" + FileAccess.GetOpenError().ToString() + "[/color]"); return 0; }
			CacheFilesX.SeekEnd();
			Dictionary HistParsed = new Dictionary();
			HistParsed.Add("FullHistory", TempHistoryALL);
			HistParsed.Add("History", TempHistoryB1);
			HistParsed.Add("WinState", XWinStat);
			string encryptedHist = ThirtyHash(HistParsed.ToString());
			CacheFilesX.StoreLine(encryptedHist.ToString());
			totalShares += 1.0F;
			successShares += 1.0F;
			CacheFilesX.Close();
			returnInt += 1;
		}
		else
		{
			totalShares += 1.0F;
			duplicateShares += 1.0F;
		}
		if (!FileAccess.FileExists(CacheFolder + "O " + StrHistoryAll + ".bin"))
		{
			using var CacheFilesO = FileAccess.Open(CacheFolder + "O " + StrHistoryAll + ".bin", FileAccess.ModeFlags.Write);
			if (CacheFilesO == null) { GD.PrintRich(Time.GetTimeStringFromSystem()+" [O] Cache Error: [color=red]"+FileAccess.GetOpenError().ToString()+"[/color]"); return 0; }
			CacheFilesO.SeekEnd();
			Dictionary HistParsed = new Dictionary();
			HistParsed.Add("FullHistory", TempHistoryALL);
			HistParsed.Add("History", TempHistoryB1);
			HistParsed.Add("WinState", OWinStat);
			string encryptedHist = ThirtyHash(HistParsed.ToString());
			CacheFilesO.StoreLine(encryptedHist.ToString());
			totalShares += 1.0F;
			successShares += 1.0F;
			CacheFilesO.Close();
			returnInt += 1;
		}
		else
		{
			totalShares += 1.0F;
			duplicateShares += 1.0F;
		}
		if (ThreadData.ContainsKey(threadId.ToString()))
		{
			int currentAdd = (int)ThreadData[threadId.ToString()];
			currentAdd += 2;
			ThreadData[threadId.ToString()] = currentAdd;
		} else
		{
			ThreadData.Add(threadId.ToString(), returnInt);
		}
		if (threadId == 0 && returnInt > 0)
		{
			EmitSignal(SignalName.SetTable, SlotsPlayed, winnerExists, winline);
		}
		return returnInt;
	}
}
