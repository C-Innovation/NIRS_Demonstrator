using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NIRS_Demonstrator
{
    /// <summary>
    /// 
    /// </summary>
    public class BrushAnimation : IDisposable
    {
        #region Dependency Properties

        #endregion

        #region Protected Members

        #endregion

        #region Private Members


        private Dictionary<Control, bool> _AnimationState;

        #endregion

        #region Public Properties

        #endregion

        #region Public Commands

        #endregion

        #region Public Events

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public BrushAnimation()
        {
            _AnimationState = new Dictionary<Control, bool>();
        }

       

        #endregion

        #region Private Callbacks

        #endregion

        #region Command Methods

        #endregion

        #region Public Methods
        public async void RunAsync(IBrush? brushStart,
                                    IBrush? brushStop,
                                    AvaloniaProperty brushProperty,
                                    Control control,
                                    double durationMs = 300,
                                    PlaybackDirection direction = PlaybackDirection.Normal)
        {
            if (!_AnimationState.ContainsKey(control))
            {
                _AnimationState.Add(control, false);
            }
            while (_AnimationState[control])
                await Task.Delay(1);
            Animation __anim = new Animation()
            {
                Duration = TimeSpan.FromMilliseconds(durationMs),
                PlaybackDirection = direction,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = default,
                        Setters =
                        {
                            new Setter(brushProperty, brushStart)
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1),
                        Setters =
                        {
                            new Setter(brushProperty, brushStop)
                        }
                    }
                }
            };
            _AnimationState[control] = true;
            control.SetValue(brushProperty, brushStop);
            await __anim.RunAsync(control);
            _AnimationState[control] = false;


        }

        public void Dispose()
        {
            _AnimationState.Clear();
            _AnimationState = null;
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Private Methods

        #endregion
    }
}
