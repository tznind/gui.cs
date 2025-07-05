# TGUI001: Describe what your rule checks

**Category:** Reliability  
**Severity:** Warning  
**Enabled by default:** Yes

## Description

When registering an event handler for `Accepting`, you should set Handled to true, this prevents other subsequent Views from responding to the same input event.

If you do not do this then you may see unpredictable behaviour such as clicking a Button resulting in another `IsDefault` button in the View also firing.

See:

- https://github.com/gui-cs/Terminal.Gui/issues/3913
- https://github.com/gui-cs/Terminal.Gui/issues/4170

## Example

### Incorrect
```csharp
// Example of code triggering the diagnostic
