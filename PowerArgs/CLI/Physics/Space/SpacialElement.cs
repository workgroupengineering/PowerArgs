﻿using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli.Physics
{
    public interface ISpacialElement : IRectangularF
    {
        Lifetime Lifetime { get; }

        void MoveTo(float x, float y, int? z = null);
    }

    public class SpacialElement : TimeFunction, ISpacialElement
    {
        public Event SizeOrPositionChanged { get; private set; } = new Event();
        public float Left { get; private set; }
        public float Top { get; private set; }
        public int ZIndex { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
        public CompositionMode CompositionMode { get; set; } = CompositionMode.BlendBackground;
        public RGB BackgroundColor { get; set; } = RGB.Red;

        public ConsoleCharacter? Pen { get; set; }

        internal SpacialElementRenderer Renderer { get; set; }

        public IRectangularF Bounds => RectangularF.Create(Left, Top, Width, Height);

        public float CenterX => Left + (Width / 2);
        public float CenterY => Top + (Height / 2);

        internal SpacialElementInternalState InternalSpacialState => InternalState as SpacialElementInternalState;

        public ObservableObject ObservableProperties { get; private set; } = new ObservableObject();

        public T GetProperty<T>(string key, Func<T> defaultValue = null)
        {
            if(defaultValue != null && ObservableProperties.ContainsKey(key) == false)
            {
                return defaultValue();
            }
            else
            {
                return ObservableProperties.Get<T>(key);
            }
        }

        public void SetProperty<T>(string key, T val) => ObservableProperties.Set(val, key);



        public SpacialElement(float w = 1, float h = 1, float x = 0, float y = 0, int z = 0)
        {
            Width = w;
            Height = h;
            Left = x;
            Top = y;
            ZIndex = z;
            this.InternalState = new SpacialElementInternalState();
        }


        
    

        public IRectangularF GetObstacleIfMovedTo(IRectangularF f, int? z = null)
        {
            var overlaps = this.GetObstacles(z).Where(e => e.EffectiveBounds().Touches(f)).ToArray();
            return overlaps.FirstOrDefault();
        }

        public void MoveTo(float x, float y, int? z = null)
        {
            Time.AssertTimeThread();

            if(float.IsNaN(x))
            {
                x = 0;
            }

            if (float.IsNaN(y))
            {
                y = 0;
            }

            this.Left = x;
            this.Top = y;
            if (z.HasValue)
            {
                this.ZIndex = z.Value;
            }

            SizeOrPositionChanged.Fire();
        }

        public void ResizeTo(float w, float h)
        {
#if DEBUG
            Time.AssertTimeThread();
#endif

            this.Width = w;
            this.Height = h;
            SizeOrPositionChanged.Fire();
        }

        public void MoveBy(float dx, float dy, int? dz = null)
        {
            var newX = Left + dx;
            var newY = Top + dy;
            int? newZ = dz.HasValue ? ZIndex + dz.Value : new Nullable<int>();
            MoveTo(newX, newY, newZ);
        }

        public void ResizeBy(float dw, float dh)
        {
            var newW = Width + dw;
            var newH = Height + dh;
            ResizeTo(newW, newH);
        }
    }

    public interface IHaveMassBounds : ISpacialElement
    {
        IRectangularF MassBounds { get; }
        bool IsPartOfMass(SpacialElement other);
        IEnumerable<SpacialElement> Elements { get; }
    }

    public static class IHaveMassBoundsEx
    {
        public static IRectangularF CalculateMassBounds(this IHaveMassBounds mass)
        {
            var left = float.MaxValue;
            var top = float.MaxValue;
            var right = float.MinValue;
            var bottom = float.MinValue;

            foreach (var part in mass.Elements.As<ISpacialElement>().Union(new ISpacialElement[] { mass }))
            {
                left = Math.Min(left, part.Left);
                top = Math.Min(top, part.Top);
                right = Math.Max(right, part.Right());
                bottom = Math.Max(bottom, part.Bottom());
            }

            var bounds = RectangularF.Create(left, top, right - left, bottom - top);
            return bounds;
        }
    }


    public interface IAmMass : ISpacialElement
    {
        IHaveMassBounds Parent { get; }
    }

    public interface IGhost
    {
        bool IsGhost { get; set; }
    }

    public class SpacialElementInternalState : TimeFunctionInternalState
    {
        internal bool Changed { get; set; }
    }
}