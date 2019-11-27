﻿/* Copyright 2017-2019 REAL.NET group
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License. */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LogoScene.ViewModels
{
    public class TurtleControlViewModel : DependencyObject
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public double TurtleWidth => Models.Constants.TurtleWidth;

        public double TurtleHeight => Models.Constants.TurtleHeight;

        [Bindable(true)]
        public double TurtleControlX
        {
            get => (double)GetValue(TurtleXProperty);
            set => SetValue(TurtleXProperty, value);
        }

        public static readonly DependencyProperty TurtleXProperty =
            DependencyProperty.Register("TurtleControlX", typeof(double), typeof(TurtleControlViewModel), new PropertyMetadata(0.0));


        [Bindable(true)]
        public double TurtleControlY
        {
            get => (double)GetValue(TurtleYProperty);
            set => SetValue(TurtleYProperty, value);
        }

        public static readonly DependencyProperty TurtleYProperty =
            DependencyProperty.Register("TurtleControlY", typeof(double), typeof(TurtleControlViewModel), new PropertyMetadata(0.0));

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
