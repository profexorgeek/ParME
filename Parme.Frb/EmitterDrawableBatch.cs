using FlatRedBall;
using FlatRedBall.Graphics;
using Parme.CSharp;
using Parme.MonoGame;

namespace Parme.Frb
{
    public class EmitterDrawableBatch : IDrawableBatch
    {
        private readonly ITextureFileLoader _textureFileLoader = new FrbTextureFileLoader();
        private readonly ParticleCamera _particleCamera = new ParticleCamera{PositiveYAxisPointsUp = true};
        private readonly ParticlePool _particlePool = new ParticlePool();
        private readonly MonoGameEmitterRenderGroup _emitterRenderGroup;
        private bool _isEmitting;
        private MonoGameEmitter _emitter;
        private IEmitterLogic _emitterLogic;
        
        public PositionedObject Parent { get; set; }

        public IEmitterLogic EmitterLogic
        {
            get => _emitterLogic;
            set
            {
                if (_emitter != null)
                {
                    _emitter.IsEmittingNewParticles = false;
                    _emitter.KillAllParticles();
                    _emitterRenderGroup.RemoveEmitter(_emitter);
                    _emitter = null;
                }

                _emitterLogic = value;
                if (value != null)
                {
                    _emitter = new MonoGameEmitter(value, _particlePool, FlatRedBallServices.GraphicsDevice, _textureFileLoader)
                    {
                        IsEmittingNewParticles = _isEmitting,
                    };
                    _emitterRenderGroup.AddEmitter(_emitter);
                }
            }
        }

        // ReSharper disable once UnusedMember.Global
        public bool IsEmitting
        {
            get => _isEmitting;
            set
            {
                _isEmitting = value;
                _emitter.IsEmittingNewParticles = value;
            }
        }
        
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public bool UpdateEveryFrame => true;

        public EmitterDrawableBatch()
        {
            _emitterRenderGroup = new MonoGameEmitterRenderGroup(FlatRedBallServices.GraphicsDevice);
            _isEmitting = true;
        }
        
        public void Draw(Camera camera)
        {
            _particleCamera.Origin = new System.Numerics.Vector2(camera.Position.X, camera.Position.Y);
            _particleCamera.PixelWidth = camera.DestinationRectangle.Width;
            _particleCamera.PixelHeight = camera.DestinationRectangle.Height;
            _particleCamera.HorizontalZoomFactor = camera.DestinationRectangle.Width / camera.OrthogonalWidth;
            _particleCamera.VerticalZoomFactor = camera.DestinationRectangle.Height / camera.OrthogonalHeight;
            
            _emitterRenderGroup.Render(_particleCamera);
        }

        public void Update()
        {
            _emitter.WorldCoordinates = Parent != null
                ? new System.Numerics.Vector2(X + Parent.X, Y + Parent.Y)
                : new System.Numerics.Vector2(X, Y);
            
            _emitter.Update(TimeManager.SecondDifference);
        }

        public void Destroy()
        {
            _emitter.Dispose();
        }

        public void KillAllParticles() => _emitter.KillAllParticles();
    }
}