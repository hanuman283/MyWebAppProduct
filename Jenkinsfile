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
                // Build .NET application
                sh '''
                    echo "Building .NET application..."
                    dotnet restore
                    dotnet publish -c Release -o ./publish
                '''
                
                // Deploy to Cloud Run
                withCredentials([file(credentialsId: 'gcp-service-account-key', variable: 'GCP_KEY')]) {
                    sh '''
                        set -e  # Exit on any error
                        
                        # Activate service account
                        echo "Activating service account..."
                        gcloud auth activate-service-account --key-file=$GCP_KEY --project=${GCP_PROJECT_ID}
                        
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
            }
        }
        
        stage('Deploy') {
            steps {
                withCredentials([file(credentialsId: 'gcp-service-account-key', variable: 'GCP_KEY')]) {
                    sh '''
                        gcloud auth activate-service-account --key-file=$GCP_KEY
                        gcloud run deploy ${IMAGE_NAME} \
                            --image gcr.io/${GCP_PROJECT_ID}/${IMAGE_NAME}:${IMAGE_TAG} \
                            --region us-central1 \
                            --platform managed \
                            --allow-unauthenticated
                    '''
                }
            }
        }
    }
    
    post {
        success {
            echo 'Deployment successful! Service URL:'
            sh '''
                gcloud run services describe ${SERVICE_NAME} \
                    --region ${REGION} \
                    --format="value(status.url)"
            '''
        }
        failure {
            echo 'Build or deployment failed! Check the logs above for details.'
        }
    }
    
    post {
        success {
            echo 'Deployment successful! Service URL:'
            sh '''
                gcloud run services describe ${IMAGE_NAME} \
                    --region us-central1 \
                    --format="value(status.url)"
            '''
        }
    }
}