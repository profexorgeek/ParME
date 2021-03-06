using System;
using System.Linq;
using ImGuiNET;
using Parme.Core.Initializers;
using Parme.Editor.Commands;

namespace Parme.Editor.Ui.Elements.Editors.Initializers.ColorMultiplier
{
    [EditorForType(typeof(StaticColorInitializer))]
    public class StaticColorMultiplierEditor : SettingsEditorBase
    {
        [SelfManagedProperty]
        public int RedMultiplier
        {
            get => Get<int>();
            set => Set(value);
        }
        
        [SelfManagedProperty]
        public int GreenMultiplier
        {
            get => Get<int>();
            set => Set(value);
        }
        
        [SelfManagedProperty]
        public int BlueMultiplier
        {
            get => Get<int>();
            set => Set(value);
        }
        
        [SelfManagedProperty]
        public float AlphaMultiplier
        {
            get => Get<float>();
            set => Set(value);
        }
        
        protected override void CustomRender()
        {
            var red = RedMultiplier;
            var green = GreenMultiplier;
            var blue = BlueMultiplier;
            var alpha = AlphaMultiplier;
            
            ImGui.TextWrapped("Determines the color multiplier the particle will start with.");
            ImGui.NewLine();

            if (ImGui.SliderInt("Red", ref red, 0, 255))
            {
                RedMultiplier = red;
            }
            
            if (ImGui.SliderInt("Green", ref green, 0, 255))
            {
                GreenMultiplier = green;
            }
            
            if (ImGui.SliderInt("Blue", ref blue, 0, 255))
            {
                BlueMultiplier = blue;
            }
            
            if (ImGui.SliderFloat("Alpha", ref alpha, 0, 1))
            {
                AlphaMultiplier = alpha;
            }
        }

        protected override void OnNewSettingsLoaded()
        {
            var initializer = (EmitterSettings.Initializers ?? Array.Empty<IParticleInitializer>())
                .FirstOrDefault(x => x.InitializerType == InitializerType.ColorMultiplier);

            if (initializer == null)
            {
                RedMultiplier = 255;
                GreenMultiplier = 255;
                BlueMultiplier = 255;
                AlphaMultiplier = 1;
            }
            else
            {
                var colorInitializer = (StaticColorInitializer) initializer;
                RedMultiplier = colorInitializer.Red;
                GreenMultiplier = colorInitializer.Green;
                BlueMultiplier = colorInitializer.Blue;
                AlphaMultiplier = colorInitializer.Alpha;
            }
        }

        protected override void OnSelfManagedPropertyChanged(string propertyName)
        {
            var initializer = new StaticColorInitializer
            {
                Red = (byte) RedMultiplier,
                Green = (byte) GreenMultiplier,
                Blue = (byte) BlueMultiplier,
                Alpha = AlphaMultiplier,
            };
            
            CommandHandler.Execute(new UpdateInitializerCommand(InitializerType.ColorMultiplier, initializer));
        }
    }
}