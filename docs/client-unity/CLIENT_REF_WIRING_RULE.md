# Client Ref Wiring Rule

- Tuyet doi khong auto-wire ref trong client.
- Khong dung `GetComponent`, `GetComponentInChildren`, `Find`, `FindObject*`, hoac `AddComponent` lam fallback de che ref thieu trong scene/prefab.
- Ref runtime phai duoc keo qua Inspector hoac di qua singleton/service da la source of truth.
- Ref bat buoc thieu phai log `ClientLog.Error` ro de scene/prefab lo loi setup.
- Khi tao prefab moi, them san component bat buoc vao prefab thay vi de presenter/controller tu them bang runtime code.
