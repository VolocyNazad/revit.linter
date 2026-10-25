# Revit Linter

The vocabulary this repository shares between its code, its documentation and its tests. It names the product concepts, not the code that implements them.

## Onboarding

**Onboarding**:
The first-use experience that introduces Revit Linter and offers its optional setup; it spans the welcome wizard and the practical tour.
_Avoid_: Welcome experience, first-run flow, tutorial flow

**Onboarding state**:
The per-user record of which onboarding steps a user has seen, which examples were offered and how far the practical tour progressed.
_Avoid_: Welcome settings, tour progress file

**Welcome wizard**:
The modal, one-shot flow that shows the setup steps a user has not seen yet, installs the optional example configuration and decides whether the practical tour starts.
_Avoid_: Welcome window, welcome dialog, intro wizard

**Practical tour**:
The optional modeless guided session that follows the real workflow and advances only from actions the user has already performed; it never runs diagnostics, changes the model or applies fixes by itself.
_Avoid_: Walkthrough, tutorial, guided mode, onboarding tour

**Training check**:
The managed diagnostic that gives the practical tour a real finding to inspect; it exists only for the tour and is removed when the tour completes or the user opts out.
_Avoid_: Demo diagnostic, test rule, tutorial diagnostic

**Managed configuration file**:
A diagnostic configuration file whose content and lifetime the product owns: it is installed, refreshed in the current format and removed without the user editing it.
_Avoid_: Generated configuration, system configuration, installed configuration

**Example configuration**:
The optional configuration shipped with the product and installed on request to demonstrate built-in and user-defined rules.
_Avoid_: Sample configuration, preset, template configuration

**Tutorial sample**:
A disposable copy of an official Autodesk sample model that the practical tour starts from; the official sample itself is never opened for modification.
_Avoid_: Demo model, example project, training file
