pipeline {
    agent any
    
    environment {
        GCP_PROJECT_ID = credentials('gcp-project-id')
        SERVICE_NAME = "mywebappproduct"
        REGION = "us-central1"
    }
    
    stages {
        stage('Build and Deploy') {
            steps {
                script {
                        try {
                            // Verify required roles
                            withCredentials([file(credentialsId: 'gcp-service-account-key', variable: 'GCP_KEY')]) {
                                sh '''
                                    echo "Verifying service account permissions..."
                                    gcloud auth activate-service-account --key-file=$GCP_KEY --project=${GCP_PROJECT_ID}
                                    
                                    # Check if service account has required roles
                                    SA_EMAIL=$(gcloud auth list --filter=status:ACTIVE --format="value(account)")
                                    echo "Checking permissions for: $SA_EMAIL"
                                    
                                    required_roles=(
                                        "roles/run.admin"
                                        "roles/storage.admin"
                                        "roles/cloudbuild.builds.builder"
                                    )
                                    
                                    for role in "${required_roles[@]}"; do
                                        if ! gcloud projects get-iam-policy ${GCP_PROJECT_ID} \
                                            --format="table(bindings.members)" \
                                            --filter="bindings.role:$role" | grep -q $SA_EMAIL; then
                                            echo "Warning: Service account missing role: $role"
                                            echo "Please grant the role using:"
                                            echo "gcloud projects add-iam-policy-binding ${GCP_PROJECT_ID} --member=serviceAccount:$SA_EMAIL --role=$role"
                                        fi
                                    done
                                '''
                            }
                            
                            // Build .NET application
                            sh '''
                                echo "Building .NET application..."
                                dotnet restore
                                dotnet publish -c Release -o ./publish
                            '''                        // Deploy to Cloud Run
                        withCredentials([file(credentialsId: 'gcp-service-account-key', variable: 'GCP_KEY')]) {
                            sh '''
                                set -e  # Exit on any error
                                
                                # Activate service account
                                echo "Activating service account..."
                                gcloud auth activate-service-account --key-file=$GCP_KEY --project=${GCP_PROJECT_ID}
                                
                                # Enable required APIs
                                echo "Enabling required GCP APIs..."
                                gcloud services enable run.googleapis.com --project=${GCP_PROJECT_ID}
                                gcloud services enable cloudbuild.googleapis.com --project=${GCP_PROJECT_ID}
                                
                                # Wait for API activation to propagate
                                echo "Waiting for APIs to be fully enabled..."
                                sleep 30
                                
                                # Deploy directly to Cloud Run using buildpacks (no Docker needed)
                                echo "Building and deploying to Cloud Run..."
                                gcloud run deploy ${SERVICE_NAME} \
                                    --source . \
                                    --region ${REGION} \
                                    --platform managed \
                                    --allow-unauthenticated \
                                    --set-env-vars="ASPNETCORE_ENVIRONMENT=Production"
                            '''
                        }
                    } catch (Exception e) {
                        echo "Error during build or deployment: ${e.message}"
                        throw e
                    }
                }
            }
        }
    }
    
    post {
        success {
            echo 'Deployment successful! Getting service URL...'
            script {
                withCredentials([file(credentialsId: 'gcp-service-account-key', variable: 'GCP_KEY')]) {
                    sh '''
                        gcloud auth activate-service-account --key-file=$GCP_KEY --project=${GCP_PROJECT_ID}
                        echo "Service URL:"
                        gcloud run services describe ${SERVICE_NAME} \
                            --region ${REGION} \
                            --format="value(status.url)"
                    '''
                }
            }
        }
        failure {
            echo 'Build or deployment failed! Check the logs above for details.'
        }
    }
}