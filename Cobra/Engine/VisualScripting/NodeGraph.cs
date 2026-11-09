using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Cobra.Engine.VisualScripting
{
    public class NodeTemplateDefinition
    {
        public string Category { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, string> DefaultProperties { get; set; } = [];
    }

    public class NodeGraph
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public string GraphName { get; set; } = "GameLogicGraph";
        public int Version { get; set; } = 2;
        public List<Node> Nodes { get; set; } = [];

        public void Save(string filePath)
        {
            string? dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(filePath, JsonSerializer.Serialize(this, JsonOptions));
        }

        public static NodeGraph Load(string filePath)
        {
            if (!File.Exists(filePath)) return new NodeGraph();
            string json = File.ReadAllText(filePath);

            try
            {
                if (json.TrimStart().StartsWith('['))
                {
                    var legacyNodes = JsonSerializer.Deserialize<List<Node>>(json) ?? [];
                    return new NodeGraph { Nodes = legacyNodes };
                }

                return JsonSerializer.Deserialize<NodeGraph>(json) ?? new NodeGraph();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return new NodeGraph();
            }
        }

        public (bool IsValid, List<string> Errors) ValidateGraph()
        {
            var errors = new List<string>();
            if (Nodes.Count == 0)
            {
                errors.Add("Graph contains no nodes.");
                return (false, errors);
            }

            var nodeIds = new HashSet<string>(Nodes.Select(n => n.Id));
            bool hasEntryEvent = Nodes.Any(n => n.Category.Equals("Events", StringComparison.OrdinalIgnoreCase)
                                             || n.Type.StartsWith("On", StringComparison.OrdinalIgnoreCase)
                                             || n.Type.Equals("Start", StringComparison.OrdinalIgnoreCase));

            if (!hasEntryEvent)
                errors.Add("Graph should include at least one Event node (e.g., OnStart, OnUpdate, OnKeyPress).");

            foreach (var node in Nodes)
            {
                foreach (var targetId in node.Outputs)
                {
                    if (!nodeIds.Contains(targetId))
                        errors.Add($"Node '{node.Title}' ({node.Id}) connects to missing target ID '{targetId}'.");
                    if (targetId == node.Id)
                        errors.Add($"Node '{node.Title}' ({node.Id}) has a self-loop connection.");
                }
            }

            return (errors.Count == 0, errors);
        }

        public string GeneratePythonCode()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Auto-generated Python script from Cobra Visual Scripting Graph");
            sb.AppendLine($"# Graph: {GraphName}");
            sb.AppendLine("import pygame");
            sb.AppendLine();
            sb.AppendLine("class VisualScriptController:");
            sb.AppendLine("    def __init__(self, entity=None):");
            sb.AppendLine("        self.entity = entity or {'x': 100.0, 'y': 100.0, 'vx': 0.0, 'vy': 0.0, 'anim': 'idle'}");
            sb.AppendLine("        self.variables = {}");
            sb.AppendLine("        self.timers = {}");
            sb.AppendLine();

            var nodeMap = Nodes.ToDictionary(n => n.Id, n => n);
            List<Node> startNodes = [.. Nodes.Where(n => n.Type is "OnStart" or "Start")];
            List<Node> updateNodes = [.. Nodes.Where(n => n.Type is "OnUpdate" or "OnKeyPress" or "OnCollision" or "Timer")];

            sb.AppendLine("    def on_start(self):");
            if (startNodes.Count == 0)
            {
                sb.AppendLine("        pass");
            }
            else
            {
                foreach (var st in startNodes)
                    EmitNodeChain(sb, st, nodeMap, [], indent: 2);
            }
            sb.AppendLine();

            sb.AppendLine("    def on_update(self, dt=0.016, keys=None):");
            if (updateNodes.Count == 0 && startNodes.Count == 0 && Nodes.Count > 0)
            {
                foreach (var n in Nodes)
                    EmitNodeChain(sb, n, nodeMap, [], indent: 2);
            }
            else if (updateNodes.Count == 0)
            {
                sb.AppendLine("        pass");
            }
            else
            {
                foreach (var up in updateNodes)
                    EmitNodeChain(sb, up, nodeMap, [], indent: 2);
            }

            return sb.ToString();
        }

        private static void EmitNodeChain(StringBuilder sb, Node node, Dictionary<string, Node> map, HashSet<string> visited, int indent)
        {
            if (!visited.Add(node.Id)) return;
            string pad = new(' ', indent * 4);

            switch (node.Type)
            {
                case "OnKeyPress":
                    string key = node.Properties.GetValueOrDefault("Key", "K_SPACE");
                    sb.AppendLine($"{pad}if keys and keys[pygame.{key}]:");
                    foreach (var nextId in node.Outputs)
                        if (map.TryGetValue(nextId, out var nextNode))
                            EmitNodeChain(sb, nextNode, map, visited, indent + 1);
                    if (node.Outputs.Count == 0) sb.AppendLine($"{pad}    pass");
                    return;

                case "Condition":
                    string expr = node.Properties.GetValueOrDefault("Expression", "True");
                    sb.AppendLine($"{pad}if {expr}:");
                    foreach (var nextId in node.Outputs)
                        if (map.TryGetValue(nextId, out var nextNode))
                            EmitNodeChain(sb, nextNode, map, visited, indent + 1);
                    if (node.Outputs.Count == 0) sb.AppendLine($"{pad}    pass");
                    return;

                case "MoveObject":
                    string dx = node.Properties.GetValueOrDefault("DeltaX", "5.0");
                    string dy = node.Properties.GetValueOrDefault("DeltaY", "0.0");
                    sb.AppendLine($"{pad}self.entity['x'] += {dx}");
                    sb.AppendLine($"{pad}self.entity['y'] += {dy}");
                    break;

                case "SetVariable":
                    string varName = node.Properties.GetValueOrDefault("Name", "score");
                    string varVal = node.Properties.GetValueOrDefault("Value", "0");
                    sb.AppendLine($"{pad}self.variables['{varName}'] = {varVal}");
                    break;

                case "Timer":
                    string timerName = node.Properties.GetValueOrDefault("TimerName", "cooldown");
                    string duration = node.Properties.GetValueOrDefault("Duration", "1.0");
                    sb.AppendLine($"{pad}self.timers['{timerName}'] = self.timers.get('{timerName}', 0.0) + dt");
                    sb.AppendLine($"{pad}if self.timers['{timerName}'] >= {duration}:");
                    sb.AppendLine($"{pad}    self.timers['{timerName}'] = 0.0");
                    foreach (var nextId in node.Outputs)
                        if (map.TryGetValue(nextId, out var nextNode))
                            EmitNodeChain(sb, nextNode, map, visited, indent + 1);
                    return;

                case "PlayAudio":
                    string soundFile = node.Properties.GetValueOrDefault("AudioPath", "assets/sfx.wav");
                    sb.AppendLine($"{pad}# Play Audio: {soundFile}");
                    sb.AppendLine($"{pad}if pygame.mixer.get_init(): pygame.mixer.Sound('{soundFile}').play()");
                    break;

                case "PlayAnimation":
                    string clip = node.Properties.GetValueOrDefault("Clip", "run");
                    sb.AppendLine($"{pad}self.entity['anim'] = '{clip}'");
                    break;

                case "LoadScene":
                    string scene = node.Properties.GetValueOrDefault("SceneName", "MainScene");
                    sb.AppendLine($"{pad}self.entity['next_scene'] = '{scene}'");
                    break;

                default:
                    sb.AppendLine($"{pad}# Node: {node.Title} ({node.Type})");
                    break;
            }

            foreach (var nextId in node.Outputs)
            {
                if (map.TryGetValue(nextId, out var next))
                    EmitNodeChain(sb, next, map, visited, indent);
            }
        }

        public static List<NodeTemplateDefinition> GetStandardLibrary()
        {
            return
            [
                new NodeTemplateDefinition { Category = "Events", Type = "OnStart", Title = "On Start", Description = "Fires once when the scene or object initializes." },
                new NodeTemplateDefinition { Category = "Events", Type = "OnUpdate", Title = "On Frame Update", Description = "Fires every game loop frame with delta time." },
                new NodeTemplateDefinition { Category = "Input", Type = "OnKeyPress", Title = "On Key Pressed", Description = "Checks if a keyboard key is pressed.", DefaultProperties = new() { ["Key"] = "K_SPACE" } },
                new NodeTemplateDefinition { Category = "Movement", Type = "MoveObject", Title = "Translate Object", Description = "Moves the game object by (DeltaX, DeltaY).", DefaultProperties = new() { ["DeltaX"] = "4.0", ["DeltaY"] = "0.0" } },
                new NodeTemplateDefinition { Category = "Conditions", Type = "Condition", Title = "Branch Condition", Description = "Evaluates a boolean Python condition.", DefaultProperties = new() { ["Expression"] = "self.entity['x'] > 0" } },
                new NodeTemplateDefinition { Category = "Variables", Type = "SetVariable", Title = "Set Variable", Description = "Sets a gameplay variable on the controller.", DefaultProperties = new() { ["Name"] = "score", ["Value"] = "10" } },
                new NodeTemplateDefinition { Category = "Timers", Type = "Timer", Title = "Interval Timer", Description = "Triggers connected nodes every N seconds.", DefaultProperties = new() { ["TimerName"] = "spawn_timer", ["Duration"] = "1.5" } },
                new NodeTemplateDefinition { Category = "Collision", Type = "OnCollision", Title = "On Collision Enter", Description = "Fires when collider overlaps target tag.", DefaultProperties = new() { ["TargetTag"] = "Enemy" } },
                new NodeTemplateDefinition { Category = "Audio", Type = "PlayAudio", Title = "Play Sound Effect", Description = "Plays an audio file using pygame.mixer.", DefaultProperties = new() { ["AudioPath"] = "assets/jump.wav" } },
                new NodeTemplateDefinition { Category = "Animation", Type = "PlayAnimation", Title = "Play Sprite Animation", Description = "Switches active animation clip.", DefaultProperties = new() { ["Clip"] = "walk" } },
                new NodeTemplateDefinition { Category = "Scene", Type = "LoadScene", Title = "Transition Scene", Description = "Switches active Cobra game scene.", DefaultProperties = new() { ["SceneName"] = "Level2" } }
            ];
        }
    }
}
