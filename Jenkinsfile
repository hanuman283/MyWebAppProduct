pipeline {
    agent any
    
    environment {
        GCP_PROJECT_ID = credentials('gcp-project-id')
        IMAGE_NAME = "mywebappproduct"
        IMAGE_TAG = "${BUILD_NUMBER}"
    }
    
    stages {
        stage('Build') {
            steps {
                // First build the .NET application
                sh 'dotnet restore'
                sh 'dotnet build --configuration Release'
                sh 'dotnet publish -c Release -o ./publish'
                
                // Then build Docker image
                script {
                    def imageUrl = "gcr.io/${GCP_PROJECT_ID}/${IMAGE_NAME}:${IMAGE_TAG}"
                    sh "docker build -t ${imageUrl} ."
                }
            }
        }
        
        stage('Push') {
            steps {
                withCredentials([file(credentialsId: 'gcp-service-account-key', variable: 'GCP_KEY')]) {
                    sh '''
                        set -e  # Exit on any error
                        
                        # Activate service account with necessary scopes
                        if ! gcloud auth activate-service-account --key-file=$GCP_KEY \
                            --project=${GCP_PROJECT_ID}; then
                            echo "Failed to activate service account"
                            exit 1
                        fi
                        
                        # Configure Docker to use gcloud as the credential helper
                        echo "Configuring Docker authentication..."
                        if ! gcloud auth configure-docker gcr.io --quiet; then
                            echo "Failed to configure Docker authentication"
                            exit 1
                        fi
                        
                        # Push the image with retries
                        echo "Pushing image to GCR..."
                        MAX_RETRIES=3
                        RETRY_COUNT=0
                        
                        while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
                            if docker push gcr.io/${GCP_PROJECT_ID}/${IMAGE_NAME}:${IMAGE_TAG}; then
                                echo "Successfully pushed image to GCR"
                                break
                            else
                                RETRY_COUNT=$((RETRY_COUNT + 1))
                                if [ $RETRY_COUNT -lt $MAX_RETRIES ]; then
                                    echo "Push failed, retrying in 10 seconds... (Attempt $RETRY_COUNT of $MAX_RETRIES)"
                                    sleep 10
                                else
                                    echo "Failed to push image after $MAX_RETRIES attempts"
                                    exit 1
                                fi
                            fi
                        done
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