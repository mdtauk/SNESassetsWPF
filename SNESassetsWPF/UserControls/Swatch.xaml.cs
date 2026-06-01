using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;




namespace SNESassetsWPF.UserControls
{
    public partial class Swatch : UserControl
    {
        #region User Control Properties

        /// <summary>
        /// Gets or sets the calculated RGBColor string.
        /// </summary>
        public String RGBColorHex
        {
            get { return (String)GetValue( RGBColorHexProperty ); }
            set { SetValue( RGBColorHexProperty , value ); }
        }

        /// <summary>
        /// Identifies the RGBColorHex dependency property
        /// </summary>
        public static readonly DependencyProperty RGBColorHexProperty = DependencyProperty.Register("RGBColorHex", typeof(String), typeof(Swatch), new PropertyMetadata(""));




        /// <summary>
        /// Gets or sets the SnesColorValue string.
        /// </summary>
        public String SnesColorValue
        {
            get { return (String)GetValue( SnesColorValueProperty ); }
            set { SetValue( SnesColorValueProperty , value ); }
        }

        /// <summary>
        /// Identifies the SnesColorValue dependency property
        /// </summary>
        public static readonly DependencyProperty SnesColorValueProperty = 
            DependencyProperty.Register("SnesColorValue", typeof(String), typeof(Swatch), new PropertyMetadata(""));




        /// <summary>
        /// Gets or sets the IsInvalidSnesColor bool.
        /// </summary>
        public bool IsInvalidSnesColor
        {
            get => (bool)GetValue( IsInvalidSnesColorProperty );
            set => SetValue( IsInvalidSnesColorProperty , value );
        }

        /// <summary>
        /// Identifies the IsInvalidSnesColor dependency property.
        /// </summary>
        /// <remarks>Default value is false. Registered on the Swatch type.</remarks>
        public static readonly DependencyProperty IsInvalidSnesColorProperty =
            DependencyProperty.Register("IsInvalidSnesColor", typeof(bool), typeof(Swatch), new PropertyMetadata(false, OnIsInvalidChanged));

        /// <summary>
        /// Sets the Visual State based on the value of the IsInvalidSnesColor bool.
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnIsInvalidChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
        {
            var swatch = (Swatch)d;
            bool isInvalid = (bool)e.NewValue;

            VisualStateManager.GoToState(
                swatch ,
                isInvalid ? "Invalid" : "Valid" ,
                true );
        }



        /// <summary>
        /// Gets or sets the IsPlaceholder bool.
        /// </summary>
        public bool IsPlaceholder
        {
            get => (bool)GetValue( IsPlaceholderProperty );
            set => SetValue( IsPlaceholderProperty , value );
        }

        /// <summary>
        /// Identifies the IsPlaceholder dependency property on the Swatch type.
        /// </summary>
        /// <remarks>Default value is false. Property changes invoke OnPlaceholderChanged.</remarks>
        public static readonly DependencyProperty IsPlaceholderProperty =
            DependencyProperty.Register(
            nameof(IsPlaceholder),
            typeof(bool),
            typeof(Swatch),
            new PropertyMetadata(false, OnPlaceholderChanged));

        /// <summary>
        /// Indicates if the colour has not been set, and so displays a different visual state
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void OnPlaceholderChanged(DependencyObject d , DependencyPropertyChangedEventArgs e)
        {
            var swatch = (Swatch)d;
            bool isPlaceholder = (bool)e.NewValue;

            VisualStateManager.GoToState(
                swatch ,
                isPlaceholder ? "Placeholder" : "Normal" ,
                true );
        }


        #endregion

        public Swatch()
        {
            InitializeComponent();
        }

    }
}
