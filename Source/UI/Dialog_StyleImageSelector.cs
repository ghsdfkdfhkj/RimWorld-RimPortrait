using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using Verse.Sound;

namespace RimPortrait
{
    public class Dialog_StyleImageSelector : Window
    {
        private Action<string> onSelect;
        private List<string> cacheFiles = new List<string>();
        private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
        private Vector2 scrollPosition;
        
        // Optimization: Loading Queue
        private Queue<string> loadQueue = new Queue<string>();
        private HashSet<string> queuedPaths = new HashSet<string>();
        // To avoid reloading failed images repeatedly
        private HashSet<string> failedPaths = new HashSet<string>();
        
        public override Vector2 InitialSize => new Vector2(600f, 600f);

        public Dialog_StyleImageSelector(Action<string> onSelectCallback)
        {
            this.onSelect = onSelectCallback;
            this.forcePause = true;
            this.doCloseX = true;
            this.doCloseButton = true;
            this.draggable = true;
            this.resizeable = true;
            LoadCacheFiles();
        }

        private void LoadCacheFiles()
        {
            cacheFiles.Clear();
            loadQueue.Clear();
            queuedPaths.Clear();
            failedPaths.Clear();
            // textureCache.Clear(); // Keep existing textures? Or clearing is safer for Refresh.
            
            string path = ImageLoader.CachePath;
            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.*")
                    .Where(s => s.EndsWith(".png") || s.EndsWith(".jpg") || s.EndsWith(".jpeg"));
                
                foreach (string f in files)
                {
                    cacheFiles.Add(f);
                }
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Title
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), "Select Style Reference Image");
            Text.Font = GameFont.Small;
            
            // Toolbar
            float topY = inRect.y + 35f;
            if (Widgets.ButtonText(new Rect(inRect.x, topY, 140f, 28f), "Open Cache Folder"))
            {
                if (!Directory.Exists(ImageLoader.CachePath)) Directory.CreateDirectory(ImageLoader.CachePath);
                Application.OpenURL(ImageLoader.CachePath);
            }
            if (Widgets.ButtonText(new Rect(inRect.x + 150f, topY, 80f, 28f), "Refresh"))
            {
                LoadCacheFiles();
            }

            // Grid
            Rect listRect = new Rect(inRect.x, topY + 35f, inRect.width, inRect.height - topY - 35f - 40f); // 40f for bottom close button space
            Widgets.DrawBoxSolid(listRect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
            Widgets.DrawBox(listRect);
            
            DrawImageGrid(listRect.ContractedBy(4f));
        }
        
        public override void WindowUpdate()
        {
            base.WindowUpdate();
            ProcessLoadQueue();
        }

        private void ProcessLoadQueue()
        {
            if (loadQueue.Count > 0)
            {
                // Process 1 image per frame to prevent freeze
                string filePath = loadQueue.Dequeue();
                queuedPaths.Remove(filePath);

                if (File.Exists(filePath) && !textureCache.ContainsKey(filePath))
                {
                    try
                    {
                        byte[] data = File.ReadAllBytes(filePath); // Still potentially slow for huge files, but better distributed
                        Texture2D tex = new Texture2D(2, 2);
                        tex.LoadImage(data);
                        textureCache[filePath] = tex;
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[RimPortrait] Failed to load image {Path.GetFileName(filePath)}: {ex.Message}");
                        failedPaths.Add(filePath);
                    }
                }
            }
        }

        private void DrawImageGrid(Rect rect)
        {
            float itemSize = 100f; 
            float gap = 8f;
            float rowHeight = itemSize + gap;
            
            int cols = Mathf.FloorToInt((rect.width - 16f) / (itemSize + gap));
            if (cols < 1) cols = 1;

            int rows = Mathf.CeilToInt((float)cacheFiles.Count / cols);
            float viewHeight = rows * rowHeight;
            
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, viewHeight);
            
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);
            
            // Virtualization Optimization
            float curY = scrollPosition.y;
            float maxY = curY + rect.height;

            int startRow = Mathf.FloorToInt(curY / rowHeight);
            int endRow = Mathf.CeilToInt(maxY / rowHeight);
            
            startRow = Mathf.Max(0, startRow);
            endRow = Mathf.Min(rows, endRow);

            int startIdx = startRow * cols;
            int endIdx = Mathf.Min(cacheFiles.Count, (endRow + 1) * cols);

            for (int i = startIdx; i < endIdx; i++)
            {
                string filePath = cacheFiles[i];
                int c = i % cols;
                int r = i / cols;
                
                Rect itemRect = new Rect(c * (itemSize + gap), r * rowHeight, itemSize, itemSize);
                
                // Texture Handling
                Texture2D tex;
                bool loaded = textureCache.TryGetValue(filePath, out tex);
                
                if (!loaded && !failedPaths.Contains(filePath))
                {
                    // Should we load it?
                    if (!queuedPaths.Contains(filePath))
                    {
                        loadQueue.Enqueue(filePath);
                        queuedPaths.Add(filePath);
                    }
                }
                
                Widgets.DrawHighlightIfMouseover(itemRect);

                if (tex != null)
                {
                    GUI.DrawTexture(itemRect, tex, ScaleMode.ScaleToFit);
                }
                else
                {
                    Widgets.DrawBoxSolid(itemRect, new Color(0.2f, 0.2f, 0.2f));
                    Text.Anchor = TextAnchor.MiddleCenter;
                    if (failedPaths.Contains(filePath))
                        Widgets.Label(itemRect, "Error");
                    else
                        Widgets.Label(itemRect, "Loading...");
                    Text.Anchor = TextAnchor.UpperLeft;
                }
                
                // Show filename in tooltip
                TooltipHandler.TipRegion(itemRect, Path.GetFileName(filePath));

                if (Widgets.ButtonInvisible(itemRect))
                {
                    onSelect?.Invoke(filePath);
                    SoundDefOf.Click.PlayOneShotOnCamera(null);
                    Close();
                }
            }
            
            Widgets.EndScrollView();
        }
    }
}
