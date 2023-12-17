//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using Windows.UI.Input;
using Windows.UI.Xaml.Input;
namespace SIUC311.Manipulations
{
    /// <summary>
    /// Thin wrapper around the <see cref="Windows.UI.Input.GestureRecognizer"/>, routes pointer events received by
    /// the manipulation target to the gesture recognizer.
    /// </summary>
    /// <remarks>
    /// Transformations during manipulations cannot be expressed in the coordinate space of the manipulation target.
    /// Instead they need be expressed with respect to a reference coordinate space, usually an ancestor (in the UI tree)
    /// of the element being manipulated.
    /// </remarks>
    public class InputProcessor
    {
        protected Windows.UI.Input.GestureRecognizer _gestureRecognizer;

        private static PointerPoint pressPoint = null;
        //private static PointerPoint previousPoint = null;

        // Element being manipulated
        protected Windows.UI.Xaml.FrameworkElement _target;
        public Windows.UI.Xaml.FrameworkElement Target
        {
            get { return _target; }
        }

        // Reference element that contains the coordinate space used for expressing transformations 
        // during manipulation, usually the parent element of Target in the UI tree
        protected Windows.UI.Xaml.Controls.Canvas _reference;
        public Windows.UI.Xaml.FrameworkElement Reference
        {
            get { return _reference; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="element">
        /// Manipulation target.
        /// </param>
        /// <param name="reference">
        /// Element that contains the coordinate space used for expressing transformations
        /// during manipulations, usually the parent element of Target in the UI tree.
        /// </param>
        /// <remarks>
        /// Transformations during manipulations cannot be expressed in the coordinate space of the manipulation target.
        /// Thus <paramref name="element"/> and <paramref name="reference"/> must be different. Usually <paramref name="reference"/>
        /// will be an ancestor of <paramref name="element"/> in the UI tree.
        /// </remarks>
        internal InputProcessor(Windows.UI.Xaml.FrameworkElement element, Windows.UI.Xaml.Controls.Canvas reference)
        {
            _target = element;
            _reference = reference;

            // Setup pointer event handlers for the element.
            // They are used to feed the gesture recognizer.    
            _target.PointerMoved += OnPointerMoved;
            _target.PointerPressed += OnPointerPressed;
            _target.PointerReleased += OnPointerReleased;
            _target.PointerCanceled += OnPointerCanceled;
            _target.PointerWheelChanged += OnPointerWheelChanged;

            CrossSlideThresholds cst = new CrossSlideThresholds();
            //cst.RearrangeStart = 10;//cst.SelectionStart = 12;
            //cst.SpeedBumpStart = 12;//cst.SpeedBumpEnd = 24;
            
            // Create the gesture recognizer            
            _gestureRecognizer = new GestureRecognizer();
            _gestureRecognizer.GestureSettings =
                GestureSettings.Hold |
                GestureSettings.ManipulationRotate |
                GestureSettings.ManipulationRotateInertia |
                GestureSettings.ManipulationScale |
                GestureSettings.ManipulationScaleInertia |
                GestureSettings.ManipulationTranslateInertia |
                GestureSettings.ManipulationTranslateX |
                GestureSettings.ManipulationTranslateY |
                GestureSettings.RightTap |
                GestureSettings.Tap |
                GestureSettings.CrossSlide;

            _gestureRecognizer.CrossSlideHorizontally = true;
            _gestureRecognizer.CrossSlideThresholds = cst;

            _gestureRecognizer.ManipulationStarted += OnManipulationStarted;
            _gestureRecognizer.ManipulationUpdated += OnManipulationUpdated;
            _gestureRecognizer.ManipulationInertiaStarting += OnManipulationInertiaStarting;
            _gestureRecognizer.ManipulationCompleted += OnManipulationCompleted;
            _gestureRecognizer.Dragging += OnDragging;
            _gestureRecognizer.Holding += OnHolding;
            _gestureRecognizer.RightTapped += OnRightTapped;
            _gestureRecognizer.Tapped += OnTapped;
            _gestureRecognizer.CrossSliding += OnCrossSliding;
        }

        #region Pointer event handlers

        private void OnPointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs args)
        {
            // Capture the pointer associated to this event
            _target.CapturePointer(args.Pointer);

            pressPoint = args.GetCurrentPoint(_target);

            // Route the event to the gesture recognizer
            _gestureRecognizer.ProcessDownEvent(pressPoint);
            
            // Mark event handled, to prevent execution of default event handlers
            args.Handled = true;
        }

        private void OnPointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs args)
        {
            // Route the events to the gesture recognizer.
            // All intermediate points are passed to the gesture recognizer in
            // the coordinate system of the reference element.
            //_gestureRecognizer.ProcessMoveEvents(args.GetIntermediatePoints(_target));

            // Mark event handled, to prevent execution of default event handlers
            //args.Handled = true;
        }

        private void OnPointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs args)
        {
            PointerPoint releasePoint = args.GetCurrentPoint(_target);

            // Route the event to the gesture recognizer
            _gestureRecognizer.ProcessUpEvent(releasePoint);

            if (SIUC311.MainPage.Current.IsReportPopupOpen())
            {   
                int horizontal_distance = 0;
                if (pressPoint != null)
                {
                    horizontal_distance = Convert.ToInt32(pressPoint.Position.X) - Convert.ToInt32(releasePoint.Position.X);
                }

                if (horizontal_distance < -5)
                {
                    //SIUC311.MainPage.Current.NotifyUser("CHANGE REPORT FORWARD", SIUC311.NotifyType.QueueMessage);
                    SIUC311.MainPage.Current.ConditionalBackwardReport();
                }
                else if (horizontal_distance > 5)
                {
                    //SIUC311.MainPage.Current.NotifyUser("CHANGE REPORT BACKWARD", SIUC311.NotifyType.QueueMessage);
                    SIUC311.MainPage.Current.ConditionalForwardReport();
                }
            }
            else
            {
                pressPoint = null;
                releasePoint = null;
            }

            // Mark event handled, to prevent execution of default event handlers
            args.Handled = true;

            // Release pointer capture on the pointer associated to this event
            _target.ReleasePointerCapture(args.Pointer);

            _gestureRecognizer.CompleteGesture();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void OnManipulationStarted(GestureRecognizer sender, ManipulationStartedEventArgs args)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void OnManipulationUpdated(GestureRecognizer sender, ManipulationUpdatedEventArgs args)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void OnManipulationCompleted(GestureRecognizer sender, ManipulationCompletedEventArgs args)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void OnManipulationInertiaStarting(GestureRecognizer sender, ManipulationInertiaStartingEventArgs args)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void OnTapped(GestureRecognizer sender, TappedEventArgs args)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void OnRightTapped(GestureRecognizer sender, RightTappedEventArgs args)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void OnHolding(GestureRecognizer sender, HoldingEventArgs args)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void OnDragging(GestureRecognizer sender, DraggingEventArgs args)
        {
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void OnCrossSliding(GestureRecognizer sender, CrossSlidingEventArgs args)
        {   
            //SIUC311.MainPage.Current.NotifyUser("SLIDE", SIUC311.NotifyType.QueueMessage);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnPointerWheelChanged(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs args)
        {
            // Obtain current point in the coordinate system of the reference element
            Windows.UI.Input.PointerPoint currentPoint = args.GetCurrentPoint(_reference);

            // Find out whether shift/ctrl buttons are pressed
            bool shift = (args.KeyModifiers & Windows.System.VirtualKeyModifiers.Shift) == Windows.System.VirtualKeyModifiers.Shift;
            bool ctrl = (args.KeyModifiers & Windows.System.VirtualKeyModifiers.Control) == Windows.System.VirtualKeyModifiers.Control;

            // Route the event to the gesture recognizer
            _gestureRecognizer.ProcessMouseWheelEvent(currentPoint, shift, ctrl);

            // Mark event handled, to prevent execution of default event handlers
            args.Handled = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnPointerCanceled(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs args)
        {
            this._gestureRecognizer.CompleteGesture();

            // Release pointer capture on the pointer associated to this event
            this._target.ReleasePointerCapture(args.Pointer);

            // Mark event handled, to prevent execution of default event handlers
            args.Handled = true;
        }

        #endregion Pointer event handlers
    }
}