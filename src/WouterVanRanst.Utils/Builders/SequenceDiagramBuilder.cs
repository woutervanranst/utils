using System.Reflection.Emit;
using System.Text;

namespace WouterVanRanst.Utils.Builders
{
    public class SequenceDiagramBuilder
    {
        public string? Title { get; set; }

        private readonly Dictionary<string, string> participants = new();
        private readonly List<(string from, string to, string label, bool async)> steps = new();

        public void AddParticipant(string key, string caption)
        {
            participants.Add(key, caption);
        }

        public void AddStep(string from, string to, string label, bool async = false)
        {
            steps.Add((from, to, label, async));
        }
        
        public override string ToString()
        {
            var seq = new StringBuilder();

            //seq.AppendLine("sequenceDiagram");
            
            if (Title is not null)
                seq.AppendLine($"title {Title}");

            seq.AppendLine();

            foreach (var participant in participants)
                seq.AppendLine($"participant \"{participant.Value}\" as {participant.Key}");

            seq.AppendLine();

            foreach (var step in steps)
            {
                if (step.async)
                    seq.AppendLine($"{step.from} ->(2) {step.to}: {step.label}");
                else
                    seq.AppendLine($"{step.from} -> {step.to}: {step.label}");
            }

            return seq.ToString();
        }
    }
}
