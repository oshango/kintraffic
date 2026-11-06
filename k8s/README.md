# Kubernetes deployment scaffold

This folder contains a minimal Kubernetes deployment for the current app structure.

## Apply

```bash
kubectl apply -f k8s/
```

## Notes

- This is a cluster deployment scaffold for the current Dockerized app.
- It assumes the container images are available in your cluster registry as `kintraffic-api:latest` and `kintraffic-ui:latest`.
- The app is still data-in-memory and not yet backed by SQL persistence.
- Use `kintraffic.local` with an ingress controller or replace the host with your own DNS/name.
