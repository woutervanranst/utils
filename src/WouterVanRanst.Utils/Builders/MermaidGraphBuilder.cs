using System.Text;
using WouterVanRanst.Utils.Extensions;

internal class GraphObject
{
    public object Key { get; }
    public string Title { get; set; }
    public string Icon { get; set; }
    public string Url { get; set; }

    public GraphObject(object key)
    {
        Key = key;
    }
}


internal interface IGraphObjectHandler
{
    void Configure(GraphObject graphObject, object instance);
}


internal class MermaidGraph
{
    public MermaidGraph(string direction = "LR")
    {
        this.direction = direction;
    }
    private readonly string direction;


    private readonly Dictionary<object, GraphObject> _graphObjects = new();
    private readonly Dictionary<object, List<GraphObject>> _childGraphObjects = new();
    private readonly List<(GraphObject From, GraphObject To, string Label)> _edges = new();
    private readonly Dictionary<Type, IGraphObjectHandler> _handlers = new();
    private readonly Dictionary<string, string> _classDefs = new();
    private readonly Dictionary<object, string> _objectClasses = new();

    
    public void AddHandler<T, THandler>()
        where T : class
        where THandler : IGraphObjectHandler, new()
    {
        var handler = new THandler();
        _handlers.Add(typeof(T), handler);
    }

    internal GraphObject AddObject(object key, object parent = null)
    {
        if (_graphObjects.ContainsKey(key))
        {
            return _graphObjects[key];
        }
        
        var graphObject = new GraphObject(key);
        _graphObjects[key] = graphObject;

        if (parent != null)
        {
            if (!_childGraphObjects.ContainsKey(parent))
            {
                _childGraphObjects[parent] = new List<GraphObject>();
            }
            _childGraphObjects[parent].Add(graphObject);
        }

        var handler = GetHandler(key);
        if (handler is not null)
        {
            handler.Configure(graphObject, key);
        }
        else
        {
            throw new InvalidOperationException($"No handler found for type '{key.GetType()}'. Please register a handler for this type.");
        }

        return graphObject;

        IGraphObjectHandler GetHandler(object key)
        {
            Type type = key.GetType();
            while (type != null)
            {
                if (_handlers.TryGetValue(type, out var handler))
                {
                    return handler;
                }

                type = type.BaseType;
            }

            return null;
        }

    }

    internal void AddEdge(object from, object to, string label = null)
    {
        var fromObject = _graphObjects[from];
        var toObject = _graphObjects[to];

        var existingEdge = _edges.FirstOrDefault(e => e.From == fromObject && e.To == toObject);
        if (existingEdge != default)
        {
            if (existingEdge.Label != label)
            {
                throw new InvalidOperationException($"An edge already exists between the given objects with a different label: {existingEdge.Label}");
            }
            // If the edge already exists with the same label, do nothing.
            return;
        }

        _edges.Add((fromObject, toObject, label));
    }


    // Styling - see https://github.com/mermaid-js/mermaid/issues/582#issuecomment-370539287, https://discourse.joplinapp.org/t/how-to-guide-for-mermaid-styling/18119/4
    // https://mermaid.js.org/syntax/stateDiagram.html#apply-classdef-styles-to-states
    /*
     * --md-primary-fg-color:        #e7285d;
       --md-primary-fg-color--light: #F7C0BD;
       --md-primary-fg-color--dark:  #ffffff;
       --md-accent-fg-color:  #0000ff;
     */
    // https://www.color-hex.com/color/ff9966
    internal void AddClassDef(string className, string style)
    {
        if (!_classDefs.ContainsKey(className))
        {
            _classDefs[className] = style;
        }
        else
        {
            throw new InvalidOperationException($"A class definition with the name '{className}' already exists.");
        }
    }

    internal void AddClass(object key, string className)
    {
        if (!_classDefs.ContainsKey(className))
        {
            throw new InvalidOperationException($"No class definition found for '{className}'. Please add a class definition first.");
        }

        if (!_objectClasses.ContainsKey(key))
        {
            _objectClasses[key] = className;
        }
        else
        {
            throw new InvalidOperationException($"An object with the key '{key}' has already been assigned a class.");
        }
    }


    public override string ToString()
    {
        var builder = new StringBuilder();
        var indent = 0;

        builder.AppendLine($"graph {direction}");

        foreach (var obj in _graphObjects.Values)
        {
            if (!_childGraphObjects.Values.Any(list => list.Contains(obj)))
            {
                RenderObject(obj, indent);
            }
        }

        foreach (var edge in _edges)
        {
            var fromKey = GetNestedKey(edge.From);
            var toKey = GetNestedKey(edge.To);
            builder.AppendLine($"{fromKey} --> {(string.IsNullOrEmpty(edge.Label) ? "" : $"|{edge.Label}| ")}{toKey}");
        }

        foreach (var classDef in _classDefs)
        {
            builder.AppendLine($"classDef {classDef.Key} {classDef.Value}");
        }

        foreach (var obj in _graphObjects.Values)
        {
            var nestedKey = GetNestedKey(obj);
            if (_objectClasses.TryGetValue(obj.Key, out var className))
            {
                builder.AppendLine($"class {nestedKey} {className}");
            }
        }

        return builder.ToString();

        void RenderObject(GraphObject obj, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 2);
            var nestedKey = GetNestedKey(obj);

            if (_childGraphObjects.TryGetValue(obj.Key, out var children))
            {
                builder.AppendLine($"{indent}subgraph {nestedKey}[\"{(string.IsNullOrEmpty(obj.Icon) ? "" : $"{obj.Icon} ")}{obj.Title}\"]");
                foreach (var child in children)
                {
                    RenderObject(child, indentLevel + 1);
                }
                builder.AppendLine($"{indent}end");
            }
            else
            {
                builder.AppendLine($"{indent}{nestedKey}[\"{(string.IsNullOrEmpty(obj.Icon) ? "" : $"{obj.Icon} ")}{obj.Title}\"]");
            }
        }
    }

    private string GetNestedKey(GraphObject obj)
    {
        var parent = _childGraphObjects.FirstOrDefault(x => x.Value.Contains(obj)).Key;
        if (parent != null)
        {
            return $"{_graphObjects[parent].Title.ToKebabCase()}.{obj.Title.ToKebabCase()}";
        }
        return obj.Title.ToKebabCase();
    }
}

