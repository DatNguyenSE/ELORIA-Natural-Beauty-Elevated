# Cấu hình GitHub Actions deploy lên ECS

Workflow `.github/workflows/deploy-ecs.yml` build Angular vào `SportZone.API/API/wwwroot`, build một Docker image chứa cả frontend và backend, push image lên ECR rồi cập nhật ECS service.

Workflow chạy tự động khi merge/push vào `main`, hoặc chạy thủ công bằng **Actions → Deploy SportZone to Amazon ECS → Run workflow**.

## 1. Các tên tài nguyên workflow đang dùng

Các giá trị đang khớp với tài liệu triển khai hiện tại:

- Region: `ap-southeast-1`
- ECR repository: `sportzone-api`
- ECS cluster: `sportzone-cluster`
- ECS service: `sportzone-service`
- ECS task definition family: `sportzone-api`
- Tên container trong task definition: `sportzone-web`

Nếu tên thật trên AWS khác, sửa khối `env` trong workflow trước khi chạy.

## 2. Tạo GitHub OIDC provider trong AWS

Trong AWS Console, mở **IAM → Identity providers → Add provider**:

- Provider type: `OpenID Connect`
- Provider URL: `https://token.actions.githubusercontent.com`
- Audience: `sts.amazonaws.com`

Mỗi AWS account chỉ cần tạo provider này một lần.

## 3. Tạo IAM role cho GitHub Actions

Tạo role, ví dụ `sportzone-github-deploy`, với trust policy dưới đây. Thay `AWS_ACCOUNT_ID` bằng account ID thật.

Repository `ELORIA-Natural-Beauty-Elevated` được tạo sau ngày 15/07/2026 nên GitHub phát hành immutable OIDC subject có thêm numeric owner ID và repository ID. Lấy hai ID bằng GitHub CLI:

```powershell
gh api repos/DatNguyenSE/ELORIA-Natural-Beauty-Elevated --jq '"owner_id=\(.owner.id) repo_id=\(.id)"'
```

Thay `GITHUB_OWNER_ID` và `GITHUB_REPOSITORY_ID` trong trust policy bằng hai số nhận được.

Workflow dùng GitHub Environment tên `production`, vì vậy `sub` phải kết thúc bằng `environment:production`.

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Federated": "arn:aws:iam::AWS_ACCOUNT_ID:oidc-provider/token.actions.githubusercontent.com"
      },
      "Action": "sts:AssumeRoleWithWebIdentity",
      "Condition": {
        "StringEquals": {
          "token.actions.githubusercontent.com:aud": "sts.amazonaws.com",
          "token.actions.githubusercontent.com:sub": "repo:DatNguyenSE@GITHUB_OWNER_ID/ELORIA-Natural-Beauty-Elevated@GITHUB_REPOSITORY_ID:environment:production"
        }
      }
    }
  ]
}
```

Gắn permission policy dưới đây vào role. Thay `AWS_ACCOUNT_ID`, `ECS_TASK_EXECUTION_ROLE_NAME` và `ECS_TASK_ROLE_NAME` bằng giá trị thật. Nếu task definition không có task role riêng, xóa ARN thứ hai trong `PassEcsRoles`.

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "EcrLogin",
      "Effect": "Allow",
      "Action": "ecr:GetAuthorizationToken",
      "Resource": "*"
    },
    {
      "Sid": "PushSportZoneImage",
      "Effect": "Allow",
      "Action": [
        "ecr:BatchCheckLayerAvailability",
        "ecr:CompleteLayerUpload",
        "ecr:InitiateLayerUpload",
        "ecr:PutImage",
        "ecr:UploadLayerPart"
      ],
      "Resource": "arn:aws:ecr:ap-southeast-1:AWS_ACCOUNT_ID:repository/sportzone-api"
    },
    {
      "Sid": "ReadAndRegisterTaskDefinition",
      "Effect": "Allow",
      "Action": [
        "ecs:DescribeTaskDefinition",
        "ecs:RegisterTaskDefinition"
      ],
      "Resource": "*"
    },
    {
      "Sid": "DeploySportZoneService",
      "Effect": "Allow",
      "Action": [
        "ecs:DescribeServices",
        "ecs:UpdateService"
      ],
      "Resource": "arn:aws:ecs:ap-southeast-1:AWS_ACCOUNT_ID:service/sportzone-cluster/sportzone-service"
    },
    {
      "Sid": "PassEcsRoles",
      "Effect": "Allow",
      "Action": "iam:PassRole",
      "Resource": [
        "arn:aws:iam::AWS_ACCOUNT_ID:role/ECS_TASK_EXECUTION_ROLE_NAME",
        "arn:aws:iam::AWS_ACCOUNT_ID:role/ECS_TASK_ROLE_NAME"
      ],
      "Condition": {
        "StringEquals": {
          "iam:PassedToService": "ecs-tasks.amazonaws.com"
        }
      }
    }
  ]
}
```

## 4. Cấu hình GitHub repository

Trong GitHub repository, mở **Settings → Environments → New environment** và tạo environment tên chính xác là `production`.

Trong environment `production`, thêm **Environment variable**:

```text
AWS_DEPLOY_ROLE_ARN=arn:aws:iam::AWS_ACCOUNT_ID:role/sportzone-github-deploy
```

Đây là ARN định danh, không phải credential bí mật. Workflow không cần `AWS_ACCESS_KEY_ID` hoặc `AWS_SECRET_ACCESS_KEY`.

Có thể bật **Required reviewers** cho environment `production` nếu muốn mỗi lần deploy phải được duyệt.

## 5. Điều kiện ECS phải có trước lần chạy đầu tiên

- ECR repository `sportzone-api` đã tồn tại.
- ECS cluster, task definition và service đã chạy được ít nhất một revision.
- Task definition có container tên chính xác `sportzone-web` và container port `8080`.
- ECS task execution role đọc được image ECR, ghi CloudWatch Logs và đọc các secret runtime cần thiết.
- ALB target group trỏ tới service/container port `8080` và health check đang hoạt động.
- Runtime secrets được inject từ Secrets Manager vào task definition; không lưu secret production trong GitHub workflow.

## 6. Cảnh báo credential hiện có trong repository

`SportZone.API/API/appsettings.json` hiện chứa database password, JWT key, Cloudinary secret, email password và VNPay secret dạng plaintext. Trước khi deploy production:

1. Rotate/revoke toàn bộ credential đã commit.
2. Đưa giá trị mới vào AWS Secrets Manager.
3. Map từng secret vào ECS task definition bằng environment variable .NET tương ứng, ví dụ `ConnectionStrings__DefaultConnection`, `TokenKey`, `CloudinarySettings__ApiSecret`, `EmailSettings__Password` và `VnPay__HashSecret`.
4. Xóa giá trị bí mật khỏi file được Git theo dõi và cân nhắc làm sạch lịch sử Git nếu repository từng được chia sẻ.

Không chạy production bằng các credential đang có trong lịch sử repository.

## 7. Chạy và kiểm tra

Merge workflow vào `main`, sau đó mở tab **Actions** để theo dõi. Mỗi lần deploy tạo image tag bằng commit SHA, nên có thể xác định và rollback chính xác revision nào đã chạy.

Sau khi job thành công, kiểm tra:

- ECS service đạt trạng thái stable và task mới đang `RUNNING`.
- Target group báo `Healthy`.
- Trang chủ tải được.
- Refresh trực tiếp một Angular route con vẫn trả về ứng dụng.
- API đăng nhập, database, upload ảnh, email và luồng thanh toán dùng đúng cấu hình production.
