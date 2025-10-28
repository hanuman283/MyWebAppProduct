This project contains helper files to build and deploy the ASP.NET Core Web API to Google Cloud.

Files added:
- Dockerfile: multi-stage Dockerfile to build and run the app on .NET 8 runtime (listens on port 8080).
- cloudbuild.yaml: Google Cloud Build configuration to build the container, push it, and deploy to Cloud Run (managed) in us-central1.
- k8s/deployment.yaml: Kubernetes Deployment manifest to run the container in GKE.
- k8s/service.yaml: Kubernetes Service (LoadBalancer) exposing the app on port 80 -> container 8080.

Quick steps (gcloud must be installed and authenticated):

1. Build and push using Cloud Build (from project root):

```powershell
gcloud builds submit --config=cloudbuild.yaml .
```

2. Or build and push locally:

```powershell
docker build -t gcr.io/PROJECT_ID/mywebappproduct:latest .
docker push gcr.io/PROJECT_ID/mywebappproduct:latest
```

3. Deploy to Cloud Run (if not using cloudbuild deploy step):

```powershell
gcloud run deploy mywebappproduct --image gcr.io/PROJECT_ID/mywebappproduct:latest --region us-central1 --platform managed --allow-unauthenticated
```

4. Deploy to GKE:

```powershell
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
```

Notes:
- Replace PROJECT_ID with your GCP project id.
- For Cloud Run private services, remove --allow-unauthenticated and set IAM for invoker.
- The app listens on port 8080 and the Dockerfile sets ASPNETCORE_URLS accordingly.
