using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using TextureSwapper.Config;

namespace TextureSwapper.IO
{
	public sealed class FileWatcher : IDisposable
	{
		private readonly string directory;
		private readonly FileSystemWatcher watcherPng;
		private readonly FileSystemWatcher watcherJpg;
		private readonly FileSystemWatcher watcherJpeg;
		private readonly ConcurrentQueue<string> changedQueue = new ConcurrentQueue<string>();
		private volatile bool started;

		public FileWatcher(string directory)
		{
			this.directory = directory;
			watcherPng = CreateWatcher("*.png");
			watcherJpg = CreateWatcher("*.jpg");
			watcherJpeg = CreateWatcher("*.jpeg");
		}

		public void Start()
		{
			if (started) return;
			started = true;
			if (!Preferences.LiveReload.Value) return;
			Enable(watcherPng);
			Enable(watcherJpg);
			Enable(watcherJpeg);
			if (Preferences.DebugEnabled)
				MelonLoader.MelonLogger.Msg($"[FileWatcher] Started watching: {directory}");
		}

		private FileSystemWatcher CreateWatcher(string filter)
		{
			var fsw = new FileSystemWatcher(directory, filter)
			{
				IncludeSubdirectories = true,
				NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size
			};
			fsw.Changed += OnChanged;
			fsw.Created += OnChanged;
			fsw.Deleted += OnChanged;
			fsw.Renamed += OnRenamed;
			return fsw;
		}

		private void Enable(FileSystemWatcher fsw)
		{
			try
			{
				fsw.EnableRaisingEvents = true;
			}
			catch
			{
				// ignore
			}
		}

		private void OnChanged(object sender, FileSystemEventArgs e)
		{
			if (string.IsNullOrEmpty(e.FullPath)) return;
			changedQueue.Enqueue(e.FullPath);
			if (Preferences.DebugEnabled)
				MelonLoader.MelonLogger.Msg($"[FileWatcher] {e.ChangeType}: {e.FullPath}");
		}

		private void OnRenamed(object sender, RenamedEventArgs e)
		{
			if (!string.IsNullOrEmpty(e.OldFullPath)) changedQueue.Enqueue(e.OldFullPath);
			if (!string.IsNullOrEmpty(e.FullPath)) changedQueue.Enqueue(e.FullPath);
			if (Preferences.DebugEnabled)
				MelonLoader.MelonLogger.Msg($"[FileWatcher] Renamed: {e.OldFullPath} -> {e.FullPath}");
		}

		public List<string> DrainChangedPaths()
		{
			var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			while (changedQueue.TryDequeue(out var path))
			{
				set.Add(path);
			}
			if (set.Count == 0) return null;
			if (Preferences.DebugEnabled)
				MelonLoader.MelonLogger.Msg($"[FileWatcher] Drained {set.Count} path(s)");
			return new List<string>(set);
		}

		public void Dispose()
		{
			try { watcherPng?.Dispose(); } catch { }
			try { watcherJpg?.Dispose(); } catch { }
			try { watcherJpeg?.Dispose(); } catch { }
		}
	}
}


