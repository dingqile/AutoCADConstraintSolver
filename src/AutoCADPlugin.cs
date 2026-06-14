using System;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;

[assembly: ExtensionApplication(typeof(AutoCADConstraintSolver.PluginEntry))]
[assembly: CommandClass(typeof(AutoCADConstraintSolver.Commands))]

namespace AutoCADConstraintSolver;

/// <summary>
/// AutoCAD plugin entry point
/// </summary>
public class PluginEntry : IExtensionApplication
{
    public void Initialize()
    {
        // Called when the plugin is loaded
        WriteMessage("AutoCAD Constraint Solver plugin loaded.");
        WriteMessage("Commands: CSSOLVE, CSADDLINE, CSADDCIRCLE, CSADDARC, CSHORIZONTAL, CSVERTICAL, CSPARALLEL, CSPERPENDICULAR, CSDISTANCE");
    }

    public void Terminate()
    {
        // Called when the plugin is unloaded
        WriteMessage("AutoCAD Constraint Solver plugin unloaded.");
    }

    private void WriteMessage(string message)
    {
        try
        {
            Application.DocumentManager.MdiActiveDocument?.Editor?.WriteMessage($"\n{message}");
        }
        catch
        {
            // Ignore if AutoCAD is not available
        }
    }
}