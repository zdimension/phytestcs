using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Text;
using MoreLinq;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using TGUI;
using static phytestcs.Tools;

namespace phytestcs.Interface.Windows
{
    public class WndConsole : ChildWindowEx
    {
        private static readonly MefHostServices Host = MefHostServices.Create(MefHostServices.DefaultAssemblies);
        public static readonly AdhocWorkspace Workspace = new AdhocWorkspace(Host);

        private static readonly CSharpCompilationOptions CompilationOptions = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            usings: Scene.DefaultUsings);
        
        public EditBox Field { get; }

        public WndConsole() : base(L["Console"], 400, useLayout:true)
        {
            var cb = new ChatBox() { SizeLayout = new Layout2d("parent.iw", "parent.ih")};
            cb.SizeLayout = new Layout2d("parent.iw - 2 * x", "400");
            cb.Position = new Vector2f(4, 2);
            Add(cb, "cb");
            var w = new Group();
            Field = new EditBox();
            Field.Renderer.Font = Ui.FontMono;
            cb.Renderer.Font = Ui.FontMono;
            var btn = new BitmapButton() { Image = new Texture("icons/small/accept.png") };
            w.Add(Field, "txt");
            w.Add(btn, "btn");
            btn.Size = new Vector2f(22, 22);
            w.SizeLayout = new Layout2d("parent.iw", "btn.h");
            Field.SizeLayout = new Layout2d("btn.left - 5", "btn.h");
            btn.PositionLayout = new Layout2d("parent.iw - w - 10", "0");
            w.PositionLayout = new Layout2d("5", "cb.bottom + 5");
            Add(w, "cont");

            var commandHistory = new List<string>();
            var historyPos = 0;

            var numAutocompleteLines = 0;

            ScriptState<object?> state = null!;
            
            void ProcessCommand()
            {
                Task.Run(async () =>
                {
                    RemoveAutocompleteLines();

                    var code = Field.Text;
                    Field.Text = "";
                
                    cb.AddLine("> " + code);
                    try
                    {
                        state = await (state == null
                                ? code.Exec<object?>()
                                : state.ContinueWithAsync<object?>(code)
                            ).ConfigureAwait(true);
                        if (state.ReturnValue != null)
                        {
                            cb.AddLine(state.ReturnValue.Repr(), Color.Blue);
                        }
                    }
                    catch (Exception e)
                    {
                        cb.AddLine(e.Message, Color.Red);
                    }

                    if (historyPos != commandHistory.Count)
                    {
                        commandHistory.RemoveRange(historyPos, commandHistory.Count - historyPos);
                    }
                
                    commandHistory.Add(code);
                    historyPos++;
                
                    Field.Focus = true;
                });
            }

            void RemoveAutocompleteLines()
            {
                var count = (int)cb!.GetLineAmount();
                for (var i = count - 1; i >= count - numAutocompleteLines; i--)
                {
                    cb.RemoveLine((uint)i);
                }

                numAutocompleteLines = 0;
            }

            Field.ReturnKeyPressed += delegate { ProcessCommand(); };
            btn.Clicked += delegate { ProcessCommand(); };
            Field.OnKeyPressed(e =>
            {
                if (e.Code == Keyboard.Key.Up)
                {
                    if (historyPos > 0)
                        historyPos--;
                    Field.Text = commandHistory[historyPos];
                }
                else if (e.Code == Keyboard.Key.Down)
                {
                    if (historyPos < commandHistory.Count)
                    {
                        historyPos++;
                        Field.Text = historyPos < commandHistory.Count 
                            ? commandHistory[historyPos] 
                            : "";
                    }
                }
                else if (e.Code == Keyboard.Key.Tab)
                {
                    //Task.Run(async () =>
                    {
                        RemoveAutocompleteLines();
                        
                        var scriptProjectInfo = ProjectInfo.Create(
                                ProjectId.CreateNewId(), 
                                VersionStamp.Create(), 
                                "Script", 
                                "phytestcs", 
                                LanguageNames.CSharp, 
                                isSubmission: true)
                            .WithDefaultNamespace("phytestcs")
                            .WithMetadataReferences(Scene.DefaultReferences)
                            .WithCompilationOptions(CompilationOptions);
                        var scriptProject = Workspace.AddProject(scriptProjectInfo);

                        var position = (int) Field.CaretPosition;
                        var code = Field.Text;
                        /*if (code.Length == 0)
                        {
                            code = "phytestcs.";
                            position = code.Length;
                        }*/

                        var scriptDocumentInfo = DocumentInfo.Create(
                            DocumentId.CreateNewId(scriptProject.Id), "Script",
                            sourceCodeKind: SourceCodeKind.Script,
                            loader: TextLoader.From(TextAndVersion.Create(SourceText.From(code),
                                VersionStamp.Create())));
                        var scriptDocument = Workspace.AddDocument(scriptDocumentInfo);

                        var completionService = CompletionService.GetService(scriptDocument);

                        try
                        {
                            var results = completionService.GetCompletionsAsync(scriptDocument, position).Result;
                            if (results != null)
                            {
                                var lines = results
                                    .Items
                                    .DistinctBy(i => i.DisplayText)
                                    .Where(n => n.DisplayText.StartsWith(
                                        code[new Range(n.Span.Start, n.Span.End)],
                                        StringComparison.InvariantCultureIgnoreCase))
                                    .ToArray();

                                if (Field.Text != "")
                                {
                                    var minStr = lines.MinBy(l => l.DisplayText.Length).First();
                                    var common = new string(
                                        minStr.DisplayText
                                            .TakeWhile((c, i) => lines.All(s => s.DisplayText[i] == c)).ToArray());
                                    var newText = Field.Text[0..minStr.Span.Start];
                                    newText += common;
                                    newText += Field.Text[position..];
                                    var newPos = newText.Length;
                                    Field.Text = newText;
                                    Field.CaretPosition = (uint) newPos;
                                }

                                numAutocompleteLines = lines.Length + 1;
                                cb.AddLine("> " + Field.Text, Color.Black, Text.Styles.Italic);
                                foreach (var line in lines)
                                {
                                    cb.AddLine(line.DisplayText, new Color(128, 0, 0), Text.Styles.Italic);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            cb.AddLine("* autocompletion error", Color.Red, Text.Styles.Italic);
                            cb.AddLine(ex.Message, Color.Red, Text.Styles.Italic);
                        }

                        try
                        {
                            Workspace.CloseDocument(scriptDocument.Id);
                        }
                        catch
                        {
                            //
                        }
                    };
                }

                return false;
            });

            Focused += delegate
            {
                Field.Focus = true;
            };
        }
    }
}