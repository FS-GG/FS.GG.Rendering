# Compatibility

Compatibility impact is additive.

- Existing coordinator types from Feature 143 remain intact.
- `ControlRuntimeModel` still does not own product open, selected, value, or overlay focus state.
- New transient widget metadata rides on ordinary control attributes and is ignored by older code paths that do not collect it.
- `Controls.Elmish` effect interpretation is opt-in through new helper functions.

Migration guidance:

- Product code should continue to own `IsOpen`, selected value, and focused control state.
- Hosts that want transient behavior should collect `TransientWidgetMetadata`, validate current-frame anchors, translate to `OverlaySurface`, and route effects back to product messages.
- Disabled triggers must not be opened implicitly; use `TransientWidget.activationRequest` to preserve diagnostic behavior.
