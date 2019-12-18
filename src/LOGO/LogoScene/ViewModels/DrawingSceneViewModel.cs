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

using LogoScene.Models;
using LogoScene.Models.Log;
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
    public class DrawingSceneViewModel : ViewModelBase
    {
        public TurtleControlViewModel TurtleViewModel { get; set; }

        public Point StartPoint
        {
            get => startPoint;
            set
            {
                startPoint = value;
                OnPropertyChanged();
            }
        }

        public Point FinalPoint
        {
            get => finalPoint;
            set
            {
                finalPoint = value;
                OnPropertyChanged();
            }
        }

        public double SpeedRatio
        {
            get => speedRatio;
            set
            {
                speedRatio = value;
                OnPropertyChanged();
            }
        }

        public DrawingSceneViewModel()
        {
            Logger.InitLogger();
            this.model = new DrawingScene();
            this.commander = model.GetTurtleCommander();
            this.turtleModel = commander.Turtle;
            this.TurtleViewModel = new TurtleControlViewModel();
            this.TurtleViewModel.InitModel(turtleModel);
            this.TurtleViewModel.TurtleMovingEnded += OnTurtleMovementEnded;
            this.TurtleViewModel.TurtleRotationEnded += OnTurtleRotationEnded;
            this.TurtleViewModel.TurtleSpeedUpdateEnded += OnTurtleSpeedUpdateEnded;
            this.model.MovementOnDrawingSceneStarted += OnMovementStarted;
            this.commander.RotationStarted += OnRotationStarted;
            this.commander.SpeedUpdateStarted += OnSpeedUpdateStarted;
            this.commander.PenActionStarted += OnPenActionStarted;
            for (int i = 0; i < 4; i++)
            {
                this.model.GetTurtleCommander().MoveForward(100);
                this.model.GetTurtleCommander().RotateRight(90);
            }
            commander.SetSpeed(4);
            for (int i = 0; i < 4; i++)
            {
                this.model.GetTurtleCommander().RotateLeft(90);
                this.model.GetTurtleCommander().MoveBackward(100);
            }
        }

        public void MoveTurtle(Point startPoint, Point finalPoint)
        {
            this.StartPoint = startPoint;
            this.FinalPoint = finalPoint;
            this.TurtleViewModel.MoveTurtle(this.StartPoint, this.FinalPoint);
        }

        public void MoveTurtle(Point destination) => MoveTurtle(this.StartPoint, destination);

        public void MoveTurtle() => MoveTurtle(this.StartPoint, this.FinalPoint);

        private void OnTurtleMovementEnded(object sender, EventArgs e) => this.model.NotifyMovementPermormed();

        private void OnTurtleSpeedUpdateEnded(object sender, EventArgs e) => this.model.NotifySpeedUpdatedPerformed();

        private void OnTurtleRotationEnded(object sender, EventArgs e) => this.model.NotifyRotationPerformed();

        private void OnMovementStarted(object sender, MovementEventArgs e)
        {
            this.StartPoint = e.OldPosition;
            this.FinalPoint = e.NewPosition;
            MoveTurtle();
        }

        private void OnRotationStarted(object sender, RotationEventArgs e) => this.TurtleViewModel.UpdateAngle();

        private void OnSpeedUpdateStarted(object sender, SpeedUpdateEventArgs e) => this.TurtleViewModel.UpdateSpeed();

        private void OnPenActionStarted(object sender, PenActionEventArgs e)
        {
            throw new NotImplementedException();
        }

        private double speedRatio = 0.3;

        private Point startPoint = new Point(100, 100);

        private Point finalPoint = new Point(100, 0);

        private readonly DrawingScene model;

        private readonly ITurtleCommander commander;

        private readonly ITurtle turtleModel;
    }
}
