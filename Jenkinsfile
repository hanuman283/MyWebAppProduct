pipeline {
    agent any
    
    environment {
        GCP_PROJECT_ID = credentials('gcp-project-id')
        SERVICE_NAME = "mywebappproduct"
        REGION = "us-central1"
    }
    
    stages {
        stage('Build') {
            steps {
                sh '''
                    # Restore and publish .NET application
                    dotnet restore
                    dotnet publish -c Release -o ./publish
                '''
            }
        }
        
        stage('Deploy') {
            steps {
                withCredentials([file(credentialsId: 'gcp-service-account-key', variable: 'GCP_KEY')]) {
                    sh '''
                        set -e  # Exit on any error
                        
                        # Activate service account
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
                gcloud run services describe ${IMAGE_NAME} \
                    --region us-central1 \
                    --format="value(status.url)"
            '''
        }
    }
}