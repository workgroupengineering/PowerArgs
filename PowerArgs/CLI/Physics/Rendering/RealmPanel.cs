﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
    public class RealmPanel : ConsolePanel
    {
        public RenderLoop RenderLoop { get; private set; }

        public Size PixelSize
        {
            get
            {
                return new Physics.Size()
                {
                    H = (1.0f / this.Height) * RenderLoop.Realm.Bounds.H,
                    W = (1.0f / this.Width) * RenderLoop.Realm.Bounds.W,
                };
            }
        }

        public RealmPanel(int w, int h)
        {
            this.Background = ConsoleColor.Gray;
            this.RenderLoop = new RenderLoop(w, h);
            this.RenderLoop.ExceptionOccurred += (ex) => 
            {
                Application.QueueAction(() => 
                {
                    throw new Exception("There was an unhandled Physics exception", ex);
                });
                return false;
            };
            this.Size = new Cli.Size(w, h);
            RenderLoop.MaxFPS = 30;
            RenderLoop.Render = UpdateView;
            this.SubscribeForLifetime(nameof(Bounds), HandleResize, this.LifetimeManager);
        }

        private void HandleResize()
        {
            lock (RenderLoop.RenderLoopSync)
            {
                foreach (var renderer in RenderLoop.Renderers)
                {
                    SizeAndLocate(renderer.Value);
                }
            }
        }

        public void UpdateView()
        {
            UpdateView(false);
        }

        public void UpdateView(bool force)
        {

            UpdateViewInternal(force);
        }

        private void UpdateViewInternal(bool force)
        {
            lock (RenderLoop.RenderLoopSync)
            {
                foreach (Thing t in RenderLoop.Realm.Added)
                {
                    Application.QueueAction(() =>
                    {
                        var renderer = RenderLoop.Binder.Bind(t);
                        RenderLoop.Renderers.Add(t, renderer);
                        this.Controls.Add(renderer);
                        SizeAndLocate(renderer);
                    });
                }

                foreach (Thing t in RenderLoop.Realm.Updated)
                {
                    Application.QueueAction(() =>
                    {
                        var renderer = RenderLoop.Renderers[t];
                        SizeAndLocate(renderer);
                    });
                }

                foreach (Thing t in RenderLoop.Realm.Removed)
                {
                    var renderer = RenderLoop.Renderers[t];
                    Application.QueueAction(() =>
                    {
                        RenderLoop.Renderers.Remove(t);
                        Controls.Remove(renderer);
                    });
                }

                if (force)
                {
                    foreach (var renderer in RenderLoop.Renderers.Values)
                    {
                        Application.QueueAction(() =>
                        {
                            SizeAndLocate(renderer);
                        });
                    }
                }

                Application.Paint();
            }
        }

        private bool SizeAndLocate(ThingRenderer r)
        {
            float wPer = r.Thing.Bounds.Size.W / (float)RenderLoop.Realm.Bounds.Size.W;
            float hPer = r.Thing.Bounds.Size.H / (float)RenderLoop.Realm.Bounds.Size.H;

            float xPer = r.Thing.Bounds.Location.X / (float)RenderLoop.Realm.Bounds.Size.W;
            float yPer = r.Thing.Bounds.Location.Y / (float)RenderLoop.Realm.Bounds.Size.H;

            int x = (int)Math.Round(xPer * (float)Width);
            int y = (int)Math.Round(yPer * (float)Height);
            int w = (int)Math.Round(wPer * (float)Width);
            int h = (int)Math.Round(hPer * (float)Height);

            if(w == 0 && r.Thing.Bounds.W > 0)
            {
                w = 1;
            }

            if (h == 0 && r.Thing.Bounds.H > 0)
            {
                h = 1;
            }

            if (x != r.X || y != r.Y || w != r.Width || h != r.Height)
            {
                r.Width = w;
                r.Height = h;
#if DEBUG
                if (float.IsInfinity(x) || float.IsNaN(x) || x >= -100000 == false && x < 100000 == false) throw new Exception("Out of bounds: " + x + "," + y);
#endif

                r.X = x;
                r.Y = y;

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}