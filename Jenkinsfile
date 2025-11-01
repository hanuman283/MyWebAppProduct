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
                                    
                                    # Enable required APIs first
                                    echo "Enabling required Google Cloud APIs..."
                                    required_apis="cloudresourcemanager.googleapis.com run.googleapis.com cloudbuild.googleapis.com storage-api.googleapis.com"
                                    for api in $required_apis; do
                                        echo "Enabling $api..."
                                        if ! gcloud services enable $api --project=${GCP_PROJECT_ID}; then
                                            echo "Failed to enable $api. Please enable it manually at:"
                                            echo "https://console.developers.google.com/apis/api/$api/overview?project=${GCP_PROJECT_ID}"
                                            exit 1
                                        fi
                                    done
                                    
                                    # Wait for APIs to be fully enabled
                                    echo "Waiting for APIs to be fully enabled..."
                                    sleep 30
                                    
                                    # Check if service account has required roles
                                    SA_EMAIL=$(gcloud auth list --filter=status:ACTIVE --format="value(account)")
                                    echo "Checking permissions for: $SA_EMAIL"
                                    
                                    # Function to check and grant role
                                    check_and_grant_role() {
                                        local role=$1
                                        local description=$2
                                        echo "Checking $description role..."
                                        
                                        # First try to get current IAM policy
                                        local policy_check
                                        policy_check=$(gcloud projects get-iam-policy ${GCP_PROJECT_ID} \
                                            --format="json" 2>/dev/null || echo "")
                                            
                                        if [ -z "$policy_check" ]; then
                                            echo "WARNING: Could not check current IAM policy. Service account may need cloudresourcemanager.projects.getIamPolicy permission"
                                            # Try to grant the role anyway
                                            if gcloud projects add-iam-policy-binding ${GCP_PROJECT_ID} \
                                                --member="serviceAccount:$SA_EMAIL" \
                                                --role="$role" 2>/dev/null; then
                                                echo "Successfully granted $description role to $SA_EMAIL"
                                                return 0
                                            else
                                                echo "ERROR: Could not grant $role"
                                                echo "Please run these commands manually:"
                                                echo "1. gcloud projects add-iam-policy-binding ${GCP_PROJECT_ID} --member=serviceAccount:$SA_EMAIL --role=roles/resourcemanager.projectIamViewer"
                                                echo "2. gcloud projects add-iam-policy-binding ${GCP_PROJECT_ID} --member=serviceAccount:$SA_EMAIL --role=$role"
                                                return 1
                                            fi
                                        fi
                                        
                                        # Check if role exists in policy
                                        if echo "$policy_check" | grep -q "$role"; then
                                            if echo "$policy_check" | grep -q "$SA_EMAIL"; then
                                                echo "$description role is already granted."
                                                return 0
                                            fi
                                        fi
                                        
                                        echo "Service account missing role: $role"
                                        # Try to grant the role
                                        if gcloud projects add-iam-policy-binding ${GCP_PROJECT_ID} \
                                            --member="serviceAccount:$SA_EMAIL" \
                                            --role="$role" 2>/dev/null; then
                                            echo "Successfully granted $description role to $SA_EMAIL"
                                            return 0
                                        else
                                            echo "WARNING: Could not automatically grant $role"
                                            echo "Please run manually: gcloud projects add-iam-policy-binding ${GCP_PROJECT_ID} --member=serviceAccount:$SA_EMAIL --role=$role"
                                            return 1
                                        fi
                                    }
                                    
                                    # Initialize error counter
                                    ROLE_ERRORS=0
                                    
                                    # Check and grant Cloud Run Admin role
                                    check_and_grant_role "roles/run.admin" "Cloud Run Admin" || ROLE_ERRORS=$((ROLE_ERRORS + 1))
                                    
                                    # Check and grant Storage Admin role
                                    check_and_grant_role "roles/storage.admin" "Storage Admin" || ROLE_ERRORS=$((ROLE_ERRORS + 1))
                                    
                                    # Check and grant Cloud Build role
                                    check_and_grant_role "roles/cloudbuild.builds.builder" "Cloud Build Builder" || ROLE_ERRORS=$((ROLE_ERRORS + 1))
                                    
                                    # If any roles couldn't be granted, fail the build
                                    if [ $ROLE_ERRORS -gt 0 ]; then
                                        echo "ERROR: $ROLE_ERRORS role(s) are missing and couldn't be automatically granted."
                                        echo "Please grant the missing roles manually using the commands shown above."
                                        exit 1
                                    fi
                                    
                                    echo "All required roles are properly configured."
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