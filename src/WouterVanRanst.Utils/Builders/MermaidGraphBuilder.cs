using System.Text;

namespace WouterVanRanst.Utils.Builders;

public class GraphObject
{
    public object  SourceObject { get; }
    public string? Key          { get; set; }
    public string? Caption      { get; set; }
    public string? Icon         { get; set; }
    public string? Url          { get; set; }

    public GraphObject(object sourceObject)
    {
        SourceObject = sourceObject;
    }
}

public interface IGraphObjectHandler
{
    void Configure(GraphObject graphObject, object sourceObject);
}


public class MermaidGraph
{
    /// <summary>
    /// A MermaidGraph
    /// </summary>
    /// <param name="direction">LR or TD</param>
    public MermaidGraph(string direction = "LR")
    {
        graphDirection    = direction;
        subgraphDirection = direction;
    }

    /// <summary>
    /// A MermaidGraph
    /// </summary>
    /// <param name="graphDirection">LR or TD</param>
    /// <param name="subgraphDirection">LR or TD</param>
    public MermaidGraph(string graphDirection, string? subgraphDirection)
    {
        this.graphDirection    = graphDirection;
        this.subgraphDirection = subgraphDirection;
    }

    private readonly Dictionary<object, GraphObject>                        graphObjects      = [];
    private readonly Dictionary<object, List<GraphObject>>                  childGraphObjects = [];
    private readonly List<(GraphObject From, GraphObject To, string Label)> edges             = [];
    private readonly Dictionary<Type, IGraphObjectHandler>                  handlers          = [];
    private readonly Dictionary<string, string>                             classDefs         = [];
    private readonly Dictionary<object, string>                             objectClasses     = [];
    private readonly string                                                 graphDirection;
    private readonly string?                                                subgraphDirection;
    
    public void AddHandler<T, THandler>()
        where T : class
        where THandler : IGraphObjectHandler, new()
    {
        var handler = new THandler();
        handlers.Add(typeof(T), handler);
    }

    public void AddObject(object domainObject, object? parentDomainObject = null)
    //public GraphObject AddObject(object sourceObject, object? parentObject = null) // commented this: the graphobject shouldnt be publicly used
    {
        if (graphObjects.ContainsKey(domainObject))
            //return graphObjects[sourceObject];
            return;
        
        var graphObject = new GraphObject(domainObject);
        graphObjects[domainObject] = graphObject;

        if (parentDomainObject != null)
        {
            if (!childGraphObjects.ContainsKey(parentDomainObject))
                childGraphObjects[parentDomainObject] = new List<GraphObject>();
        
            childGraphObjects[parentDomainObject].Add(graphObject);
        }

        var handler = GetHandler(domainObject);
        if (handler is not null)
            handler.Configure(graphObject, domainObject);
        else
            throw new InvalidOperationException($"No handler found for type '{domainObject.GetType()}'. Please register a handler for this type.");

        //return graphObject;
        return;


        IGraphObjectHandler? GetHandler(object sourceDomainObject)
        {
            var type = sourceDomainObject.GetType();
            while (type != null)
            {
                if (handlers.TryGetValue(type, out var handler))
                    return handler;

                type = type.BaseType;
            }

            return null;
        }
    }

    public void AddEdge(object fromDomainObject, object toDomainObject, string label = null)
    {
        var fromObject = graphObjects[fromDomainObject];
        var toObject = graphObjects[toDomainObject];

        var existingEdge = edges.FirstOrDefault(e => e.From == fromObject && e.To == toObject);
        if (existingEdge != default)
        {
            if (existingEdge.Label != label)
                throw new InvalidOperationException($"An edge already exists between the given objects with a different label: {existingEdge.Label}");
        
            // If the edge already exists with the same label, do nothing.
            return;
        }

        edges.Add((fromObject, toObject, label));
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
    public void AddClassDef(string className, string style)
    {
        if (!classDefs.ContainsKey(className))
            classDefs[className] = style;
        else
            throw new InvalidOperationException($"A class definition with the name '{className}' already exists.");
    }

    public void AddClass(object domainObject, string className)
    {
        if (!classDefs.ContainsKey(className))
            throw new InvalidOperationException($"No class definition found for '{className}'. Please add a class definition first.");

        if (!objectClasses.ContainsKey(domainObject))
            objectClasses[domainObject] = className;
        else
            throw new InvalidOperationException($"An object '{domainObject}' has already been assigned a class.");
    }

    public IEnumerable<object> FindDomainObjects(string key)
    {
        return graphObjects.Values.Where(o => o.Key == key).Select(o => o.SourceObject);
    }


    public override string ToString()
    {
        var builder = new StringBuilder();
        var indent = 0;

        builder.AppendLine($"graph {graphDirection}");

        // Add Nodes
        if (graphObjects.Values.Select(GetNestedKey).GroupBy(v => v).Any(v => v.Count() > 1))
            throw new InvalidOperationException("Duplicate keys found. Please make sure all keys are unique.");

        foreach (var obj in graphObjects.Values)
        {
            if (!childGraphObjects.Values.Any(list => list.Contains(obj)))
                RenderObject(obj, indent);
        }

        // Add Edges
        foreach (var edge in edges)
        {
            var fromKey = GetNestedKey(edge.From);
            var toKey   = GetNestedKey(edge.To);
            builder.AppendLine($"{fromKey} --> {(string.IsNullOrEmpty(edge.Label) ? "" : $"|{edge.Label}| ")}{toKey}");
        }

        // Add ClassDefs
        foreach (var classDef in classDefs)
            builder.AppendLine($"classDef {classDef.Key} {classDef.Value}");

        // Add Styles
        foreach (var obj in graphObjects.Values)
        {
            var nestedKey = GetNestedKey(obj);
            if (objectClasses.TryGetValue(obj.SourceObject, out var className))
                builder.AppendLine($"class {nestedKey} {className}");
        }

        return builder.ToString();


        void RenderObject(GraphObject obj, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 2);
            var nestedKey = GetNestedKey(obj);

            if (childGraphObjects.TryGetValue(obj.SourceObject, out var children))
            {
                builder.AppendLine($"{indent}subgraph {nestedKey}[\"{(string.IsNullOrEmpty(obj.Icon) ? "" : $"{obj.Icon} ")}{obj.Caption}\"]");
                if (subgraphDirection is not null)
                    builder.AppendLine($"{indent}{indent}direction {subgraphDirection}");
                foreach (var child in children)
                    RenderObject(child, indentLevel + 1);
                
                builder.AppendLine($"{indent}end");
            }
            else
            {
                builder.AppendLine($"{indent}{nestedKey}[\"{(string.IsNullOrEmpty(obj.Icon) ? "" : $"{obj.Icon} ")}{obj.Caption}\"]");
            }
        }
    }

    private string? GetNestedKey(GraphObject obj)
    {
        var parentObject = childGraphObjects.FirstOrDefault(x => x.Value.Contains(obj)).Key;
        if (parentObject != null)
        {
            var parentKey = graphObjects[parentObject].Key;
;            return $"{parentKey}.{obj.Key}";
        }
        else
        {
            return obj.Key;
        }
    }
}