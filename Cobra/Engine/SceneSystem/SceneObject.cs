using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Cobra.Engine.SceneSystem
{
    public class SceneObject : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString("N")[..8];
        private string _name = "GameObject";
        private string? _parentId;
        private double _x;
        private double _y;
        private double _z;
        private double _width = 64;
        private double _height = 64;
        private double _depth = 64;
        private double _rotation;
        private double _rotationX;
        private double _rotationY;
        private double _scaleX = 1.0;
        private double _scaleY = 1.0;
        private double _scaleZ = 1.0;
        private bool _isVisible = true;
        private bool _isLocked;
        private bool _isSelected;
        private string _spritePath = string.Empty;
        private string _texturePath = string.Empty;
        private string _modelPath = string.Empty;
        private string _primitive3D = "Cube";
        private string _color = "#ff8c00";
        private string _layer = "Default";
        private int _sortingOrder;
        private string _scriptPath = string.Empty;

        // Collision & Physics Properties
        private string _colliderType = "Rectangle";
        private int _collisionLayer = 1;
        private bool _isTrigger;
        private double _colliderRadius = 32;
        private bool _hasRigidbody;
        private double _mass = 1.0;
        private double _gravityScale = 1.0;
        private bool _useGravity;
        private double _bounciness = 0.2;
        private double _friction = 0.4;

        // Animation Properties
        private string _animationClip = string.Empty;
        private int _frameCount = 1;
        private double _frameDurationMs = 100;
        private bool _loopAnimation = true;

        public string Id { get => _id; set { _id = value; OnPropertyChanged(); } }
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(HierarchyDisplay)); } }
        public string? ParentId { get => _parentId; set { _parentId = value; OnPropertyChanged(); OnPropertyChanged(nameof(HierarchyDisplay)); } }

        public double X { get => _x; set { _x = value; OnPropertyChanged(); } }
        public double Y { get => _y; set { _y = value; OnPropertyChanged(); } }
        public double Z { get => _z; set { _z = value; OnPropertyChanged(); } }

        public double Width { get => _width; set { _width = Math.Max(4, value); OnPropertyChanged(); } }
        public double Height { get => _height; set { _height = Math.Max(4, value); OnPropertyChanged(); } }
        public double Depth { get => _depth; set { _depth = Math.Max(4, value); OnPropertyChanged(); } }

        public double Rotation { get => _rotation; set { _rotation = value; OnPropertyChanged(); } }
        public double RotationX { get => _rotationX; set { _rotationX = value; OnPropertyChanged(); } }
        public double RotationY { get => _rotationY; set { _rotationY = value; OnPropertyChanged(); } }

        public double ScaleX { get => _scaleX; set { _scaleX = value; OnPropertyChanged(); } }
        public double ScaleY { get => _scaleY; set { _scaleY = value; OnPropertyChanged(); } }
        public double ScaleZ { get => _scaleZ; set { _scaleZ = value; OnPropertyChanged(); } }

        public bool IsVisible { get => _isVisible; set { _isVisible = value; OnPropertyChanged(); OnPropertyChanged(nameof(HierarchyDisplay)); } }
        public bool IsLocked { get => _isLocked; set { _isLocked = value; OnPropertyChanged(); OnPropertyChanged(nameof(HierarchyDisplay)); } }
        public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }

        public string SpritePath { get => _spritePath; set { _spritePath = value; OnPropertyChanged(); } }
        public string TexturePath { get => _texturePath; set { _texturePath = value; OnPropertyChanged(); } }
        public string ModelPath { get => _modelPath; set { _modelPath = value; OnPropertyChanged(); } }
        public string Primitive3D { get => _primitive3D; set { _primitive3D = value; OnPropertyChanged(); } }
        public string Color { get => _color; set { _color = value; OnPropertyChanged(); } }
        public string Layer { get => _layer; set { _layer = value; OnPropertyChanged(); } }
        public int SortingOrder { get => _sortingOrder; set { _sortingOrder = value; OnPropertyChanged(); } }
        public string ScriptPath { get => _scriptPath; set { _scriptPath = value; OnPropertyChanged(); } }

        public string ColliderType { get => _colliderType; set { _colliderType = value; OnPropertyChanged(); } }
        public int CollisionLayer { get => _collisionLayer; set { _collisionLayer = value; OnPropertyChanged(); } }
        public bool IsTrigger { get => _isTrigger; set { _isTrigger = value; OnPropertyChanged(); } }
        public double ColliderRadius { get => _colliderRadius; set { _colliderRadius = value; OnPropertyChanged(); } }
        public bool HasRigidbody { get => _hasRigidbody; set { _hasRigidbody = value; OnPropertyChanged(); } }
        public double Mass { get => _mass; set { _mass = Math.Max(0.01, value); OnPropertyChanged(); } }
        public double GravityScale { get => _gravityScale; set { _gravityScale = value; OnPropertyChanged(); } }
        public bool UseGravity { get => _useGravity; set { _useGravity = value; OnPropertyChanged(); } }
        public double Bounciness { get => _bounciness; set { _bounciness = Math.Clamp(value, 0, 1); OnPropertyChanged(); } }
        public double Friction { get => _friction; set { _friction = Math.Clamp(value, 0, 1); OnPropertyChanged(); } }

        public string AnimationClip { get => _animationClip; set { _animationClip = value; OnPropertyChanged(); } }
        public int FrameCount { get => _frameCount; set { _frameCount = Math.Max(1, value); OnPropertyChanged(); } }
        public double FrameDurationMs { get => _frameDurationMs; set { _frameDurationMs = Math.Max(16, value); OnPropertyChanged(); } }
        public bool LoopAnimation { get => _loopAnimation; set { _loopAnimation = value; OnPropertyChanged(); } }

        public string HierarchyDisplay
        {
            get
            {
                string prefix = !string.IsNullOrEmpty(ParentId) ? "  └─ " : "";
                string vis = IsVisible ? "" : " [Hidden]";
                string lck = IsLocked ? " [Locked]" : "";
                return $"{prefix}{Name} ({Layer}){vis}{lck}";
            }
        }

        public SceneObject Clone()
        {
            return new SceneObject
            {
                Id = Guid.NewGuid().ToString("N")[..8],
                Name = Name + "_Copy",
                ParentId = ParentId,
                X = X + 24,
                Y = Y + 24,
                Z = Z,
                Width = Width,
                Height = Height,
                Depth = Depth,
                Rotation = Rotation,
                RotationX = RotationX,
                RotationY = RotationY,
                ScaleX = ScaleX,
                ScaleY = ScaleY,
                ScaleZ = ScaleZ,
                IsVisible = IsVisible,
                IsLocked = IsLocked,
                SpritePath = SpritePath,
                TexturePath = TexturePath,
                ModelPath = ModelPath,
                Primitive3D = Primitive3D,
                Color = Color,
                Layer = Layer,
                SortingOrder = SortingOrder,
                ScriptPath = ScriptPath,
                ColliderType = ColliderType,
                CollisionLayer = CollisionLayer,
                IsTrigger = IsTrigger,
                ColliderRadius = ColliderRadius,
                HasRigidbody = HasRigidbody,
                Mass = Mass,
                GravityScale = GravityScale,
                UseGravity = UseGravity,
                Bounciness = Bounciness,
                Friction = Friction,
                AnimationClip = AnimationClip,
                FrameCount = FrameCount,
                FrameDurationMs = FrameDurationMs,
                LoopAnimation = LoopAnimation
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
