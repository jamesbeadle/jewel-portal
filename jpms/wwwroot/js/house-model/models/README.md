# Model fixtures

`ravens-dene-16.json` is the worked example of an estimate's house-model definition — 16 Ravens
Dene (EST-0001 on LD-0001), read off Resi B369214-1100 and B369214-3100 rev B — and the test
fixture the headless render check mounts. It is the same JSON that `set_house_model` stores on
the estimate; the viewer never looks a model up by name, it is handed the estimate's own
definition by `house-model.js`. The shape of the definition, and how to read one off a set of
drawings, is the `jpms-house-model` skill (docs/ai/skills/jpms/jpms-house-model.md).
