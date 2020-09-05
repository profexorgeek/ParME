﻿using System;
using ImGuiHandler;
using ImGuiHandler.MonoGame;
using ImGuiNET;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Parme.Core;
using Parme.Core.Initializers;
using Parme.Core.Modifiers;
using Parme.Core.Triggers;
using Parme.CSharp;
using Parme.CSharp.CodeGen;
using Parme.Editor.Ui;
using Parme.MonoGame;
using Vector2 = System.Numerics.Vector2;

namespace Parme.Editor
{
    public class App : Game
    {
        private const float MinSecondsForRecompilingEmitter = 0.25f;
        
        private readonly ParticleCamera _camera = new ParticleCamera();
        private readonly SettingsCommandHandler _commandHandler = new SettingsCommandHandler();
        private ITextureFileLoader _textureFileLoader;
        private MonoGameEmitter _emitter;
        private ImGuiManager _imGuiManager;
        private EditorUiController _uiController;
        private InputHandler _inputHandler;
        private float _secondsSinceLastSettingsChange;
        private bool _emitterSettingsUpdated;

        private Texture2D _testTexture;
        
        public App()
        {
            // ReSharper disable once ObjectCreationAsStatement
            new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1024,
                PreferredBackBufferHeight = 768,
                PreferMultiSampling = true
            };

            IsMouseVisible = true;
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += WindowOnClientSizeChanged;
        }

        protected override void Initialize()
        {
            _textureFileLoader = new TextureFileLoader(GraphicsDevice);
            
            _camera.Origin = new Vector2(-GraphicsDevice.Viewport.Width / 6f, GraphicsDevice.Viewport.Height / 4f);
            _camera.PositiveYAxisPointsUp = true;
            _camera.PixelWidth = GraphicsDevice.Viewport.Width;
            _camera.PixelHeight = GraphicsDevice.Viewport.Height;
            
            _imGuiManager = new ImGuiManager(new MonoGameImGuiRenderer(this));
            _uiController = new EditorUiController(_imGuiManager, _commandHandler);
            _inputHandler = new InputHandler(_uiController, _camera, _commandHandler);

            ImGui.GetIO().FontGlobalScale = 1.2f;
            _uiController.WindowResized(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            var pixels = new Color[10*10];
            for (var x = 0; x < pixels.Length; x++)
            {
                pixels[x] = Color.White;
            }
            
            _testTexture = new Texture2D(GraphicsDevice, 10, 10);
            _testTexture.SetData(pixels);

            var settings = GetInitialEmitterSettings();
            UpdateEmitter(settings);
            
            _uiController.NewEmitterSettingsLoaded(settings);

            _commandHandler.EmitterUpdated += (sender, emitterSettings) =>
            {
                _emitterSettingsUpdated = true;
                _secondsSinceLastSettingsChange = 0;
            };

            base.Initialize();
        }

        protected override void Update(GameTime gameTime)
        {
            _secondsSinceLastSettingsChange += (float) gameTime.ElapsedGameTime.TotalSeconds;

            if (_secondsSinceLastSettingsChange > MinSecondsForRecompilingEmitter && _emitterSettingsUpdated)
            {
                var settings = _commandHandler.GetCurrentSettings();
                UpdateEmitter(settings);
                
                _emitterSettingsUpdated = false;
                _secondsSinceLastSettingsChange = 0;
            }
            
            _commandHandler.UpdateTime((float) gameTime.ElapsedGameTime.TotalSeconds);
            _uiController.Update();
            _inputHandler.Update();
            _emitter?.Update((float) gameTime.ElapsedGameTime.TotalSeconds);
            
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            var backgroundColorVector = _uiController.BackgroundColor;
            var backgroundColor = new Color(backgroundColorVector.X, 
                backgroundColorVector.Y, 
                backgroundColorVector.Z);
            
            GraphicsDevice.Clear(backgroundColor);

            _emitter?.Render(_camera);
            _imGuiManager.RenderElements(gameTime.ElapsedGameTime);
            
            base.Draw(gameTime);
        }

        private static EmitterSettings GetInitialEmitterSettings()
        {
            var trigger = new TimeElapsedTrigger{Frequency = 0.01f};
            var initializers = new IParticleInitializer[]
            {
                new RandomParticleCountInitializer {MinimumToSpawn = 0, MaximumToSpawn = 5},
                new StaticColorInitializer
                {
                    // Orange
                    RedMultiplier = 1.0f,
                    GreenMultiplier = 165f / 255f,
                    BlueMultiplier = 0f,
                    AlphaMultiplier = 1f
                },

                new RandomRangeVelocityInitializer
                {
                    MinXVelocity = 0,
                    MaxXVelocity = 0,
                    MinYVelocity = 2,
                    MaxYVelocity = 5,
                },

                new RandomRegionPositionInitializer
                {
                    MinXOffset = -50,
                    MaxXOffset = 50,
                    MinYOffset = -50,
                    MaxYOffset = -50,
                },

                new StaticSizeInitializer
                {
                    Width = 50,
                    Height = 50,
                },
                
                new RandomTextureInitializer(), 
            };

            var modifiers = new IParticleModifier[]
            {
                new ConstantRotationModifier {DegreesPerSecond = 100f},
                new ConstantAccelerationModifier
                {
                    XAcceleration = -5,
                    YAcceleration = 5,
                },

                new ConstantSizeModifier
                {
                    WidthChangePerSecond = -10,
                    HeightChangePerSecond = -10,
                },

                new ConstantColorMultiplierChangeModifier
                {
                    RedMultiplierChangePerSecond = -1,
                    GreenMultiplierChangePerSecond = -1,
                    BlueMultiplierChangePerSecond = -1,
                    AlphaMultiplierChangePerSecond = -1,
                },
            };

            return new EmitterSettings
            {
                Trigger = trigger,
                Initializers = initializers,
                Modifiers = modifiers,
                MaxParticleLifeTime = 1f,
                TextureFileName = "SampleParticles.png",
                TextureSections = new []
                {
                    new TextureSectionCoords(0, 0, 15, 15),
                    new TextureSectionCoords(16, 0, 31, 15),
                    new TextureSectionCoords(32, 0, 47, 15),
                    new TextureSectionCoords(48, 0, 63, 15),
                }
            };
        }

        private void UpdateEmitter(EmitterSettings settings)
        {
            if (_emitter != null)
            {
                _emitter.Stop();
                _emitter.KillAllParticles();
                _emitter = null;
            }

            if (settings != null)
            {
                var code = EmitterLogicClassGenerator.Generate(settings, "Parme.Editor", "Test", true);
            
                var scriptOptions = ScriptOptions.Default
                    .WithReferences(typeof(IEmitterLogic).Assembly);
                
                var logicClass = CSharpScript.EvaluateAsync<IEmitterLogic>(code, scriptOptions).GetAwaiter().GetResult();

                _emitter = new MonoGameEmitter(logicClass, GraphicsDevice, _textureFileLoader)
                {
                    //WorldCoordinates = new System.Numerics.Vector2(400, -200)
                };
                _emitter.Start();
            }
        }

        private void WindowOnClientSizeChanged(object sender, EventArgs e)
        {
            _uiController.WindowResized(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            _camera.PixelWidth = GraphicsDevice.Viewport.Width;
            _camera.PixelHeight = GraphicsDevice.Viewport.Height;
        }
    }
}