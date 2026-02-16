Backend ->
1. Admin Authentication (optional)
2. Fetch Data From Prometheus for Backend (done)
3. Store the data (Metric Records) in database and Observe the Data
4. Identify The Anomalies
5. Create A Failure Event
6. Perform a Recovery Action
7. Send Request To The Kubernetes API for scaling
8. Store The Status After the Recovery Action (optional)
9. Notify The Admin

Frontend ->
1. Admin Dashboard (Done via prometheus)

Prometheus → Analyzer detects anomaly
  → Persist FailureEvent to TaskApi DB
  → Determine recovery action (RestartPod/ScaleUp)
  → Execute via Kubernetes API
  → Update recovery status in DB
  → Resolve failure event if successful
  → Notify admin (console + webhook)