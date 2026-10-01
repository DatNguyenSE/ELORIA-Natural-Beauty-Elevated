# Hướng dẫn triển khai SportZone lên AWS ECS Fargate

Cập nhật: 30/09/2026. Hướng dẫn dựa trên source SportZone hiện tại.

**Bắt đầu từ bước 0 rồi làm lần lượt. Đừng build/push image chứa cấu hình bí mật cũ.**

Tài liệu này là hướng dẫn thao tác. Các thay đổi code bên dưới chưa được tự động áp dụng và tài nguyên AWS chưa được tạo.

## Mục tiêu và những thành phần bạn sẽ dùng

- Angular được build vào `SportZone.API/API/wwwroot`.
- ASP.NET Core phục vụ cả Angular và API trong một Docker container.
- ECR lưu Docker image; ECS Fargate chạy image.
- Application Load Balancer (ALB) nhận truy cập web và chuyển vào container cổng 8080.
- RDS PostgreSQL lưu dữ liệu.
- Secrets Manager lưu connection string, JWT key và credential dịch vụ.
- Cloudinary tiếp tục lưu ảnh; chưa cần chuyển ảnh sang S3.
- CloudWatch nhận logs.
- ACM cấp certificate cho domain của bạn.

CloudFront không bắt buộc. Hai thư mục FE và BE trong cùng repository không quyết định cách deploy; cấu hình hiện tại đã hỗ trợ đóng gói chung. Nếu chỉ deploy API mà không có bản Angular trong `wwwroot`, người dùng sẽ cần một nơi khác phục vụ frontend. Hướng dẫn này deploy cả hai chung một image.

## 0. Chuẩn bị triển khai với database mới

### 0.1. Tạo RDS mới hoàn toàn

Theo phương án đã chọn, tạo một RDS PostgreSQL mới và database `sportzone` trống. Không cần kiểm tra snapshot, restore hoặc import dữ liệu từ DB cũ.

- EF Core migrations trong source sẽ tạo các bảng khi ứng dụng khởi động theo cơ chế hiện tại.
- Các hàm seed sẽ thêm dữ liệu khởi tạo có trong source; tài khoản và đơn hàng cũ không được đưa sang.
- Tiếp tục dùng Cloudinary hiện có. Kiểm tra URL ảnh trong dữ liệu seed còn hợp lệ; ảnh trên Cloudinary không tự tạo bản ghi sản phẩm trong DB mới.

### 0.2. Chọn region và cách triển khai mạng

Ví dụ xuyên suốt tài liệu dùng **Singapore — `ap-southeast-1`**. Tạo RDS, ECS, ECR, Secrets Manager và ALB trong region này. Database mới không phụ thuộc region của App Runner trước đây.

**Phương án đã chọn: không dùng NAT Gateway.** ALB và ECS task ở public subnet; ECS bật public IP. RDS ở private subnet. Security group của ECS chỉ nhận cổng 8080 từ ALB nên không mở API trực tiếp cho Internet.

ECS dùng public IP và route Internet Gateway để kéo image ECR, đọc Secrets Manager, gửi CloudWatch logs, gọi Cloudinary và SMTP. Không cần tạo VPC endpoints cho quy trình này. ECS kết nối RDS qua địa chỉ private trong cùng VPC.

ALB, Fargate, RDS, public IPv4, logs và Secrets Manager có thể tính phí. Không có chi phí NAT Gateway trong kiến trúc này. Tạo AWS Budget cảnh báo theo mức bạn chấp nhận trước khi chạy lâu dài; không mặc định cấu hình này miễn phí.

### 0.3. Cài công cụ trên máy Windows

Chuẩn bị Docker Desktop ở chế độ Linux containers, AWS CLI v2, Git, Node.js phù hợp Angular 20 và .NET SDK 9 nếu muốn build BE ngoài Docker.

Mở PowerShell và kiểm tra:

```powershell
docker version
aws --version
node --version
npm --version
git --version
```

`docker version` phải hiển thị được phần Server. Nếu không có Server, mở Docker Desktop và đợi engine chạy.

Đăng nhập CLI bằng profile được cấp quyền deploy. Nếu tài khoản dùng IAM Identity Center:

```powershell
aws configure sso --profile sportzone
aws sso login --profile sportzone
$env:AWS_PROFILE = 'sportzone'
aws sts get-caller-identity
```

Nếu không dùng Identity Center, sử dụng phương thức đăng nhập CLI mà tài khoản của bạn đã cấu hình. Không tạo access key cho root và không đưa AWS credential vào code/image.

Kiểm tra `Account` từ lệnh cuối là đúng tài khoản AWS dự định sử dụng.

### 0.4. Ghi lại các thông tin sẽ tạo

Giữ một ghi chú riêng với: region, VPC ID, subnet IDs, ba security group IDs, RDS endpoint, secret ARN, execution role ARN, ECR URI, ALB DNS, domain. Không ghi mật khẩu vào tài liệu trong Git.

Tên mẫu trong hướng dẫn:

```text
VPC:             sportzone-vpc
ALB SG:          sportzone-alb-sg
ECS SG:          sportzone-ecs-sg
RDS SG:          sportzone-rds-sg
RDS instance:    sportzone-db
Database:        sportzone
Secret:          sportzone/prod
ECR repository:  sportzone-api
ECS cluster:     sportzone-cluster
Task family:     sportzone-api
Container:       sportzone-web
ECS service:     sportzone-service
Log group:       /ecs/sportzone-api
Target group:    sportzone-web-tg
ALB:             sportzone-alb
```

## 1. Chuẩn bị source và đưa secrets ra khỏi repository

### 1.1. Thu hồi/đổi credential cũ

Cả `SportZone.API/API/appsettings.json` và `appsettings.Development.json` đang được Git theo dõi. Credential trong lịch sử Git không biến mất khi thêm `.gitignore`.

Đổi DB password, JWT signing key, Cloudinary API secret, SMTP/app password và VNPay HashSecret qua cơ chế của từng nhà cung cấp. Đổi JWT key sẽ làm token đã phát trước đó không còn hợp lệ. Không chép giá trị cũ vào hướng dẫn hoặc chat.

### 1.2. Làm sạch appsettings

Trong cả hai file, bỏ giá trị thật của các khóa sau:

```text
ConnectionStrings:DefaultConnection
TokenKey
VnPay:HashSecret
CloudinarySettings:ApiSecret
EmailSettings:Password
```

Có thể chuyển cả merchant code, email account, Cloudinary API key/cloud name sang Secrets Manager để quản lý tập trung. Các cấu hình không bí mật như SMTP host/port, VNPay version/currency/locale vẫn cần được giữ trong `appsettings.json` hoặc khai báo environment variables; Production không tự đọc `appsettings.Development.json`.

Giữ local secret bằng .NET User Secrets:

```powershell
cd D:\ADMIN\Documents\Project\SportZone
dotnet user-secrets init --project .\SportZone.API\API\SportZone.API.csproj
```

Dùng trình quản lý User Secrets của IDE để nhập giá trị local nếu có. Nếu dùng `dotnet user-secrets set`, giá trị gõ vào lệnh có thể nằm trong terminal history. User Secrets dành cho development và không phải kho mã hóa dùng cho production.

Thêm đường dẫn chính xác vào `.gitignore` nếu muốn ngừng theo dõi file development:

```gitignore
/SportZone.API/API/appsettings.Development.json
.env
.env.*
!.env.example
```

Sau khi đã lưu an toàn giá trị local, chạy lệnh sau để ngừng theo dõi file nhưng giữ file trên máy:

```powershell
git rm --cached -- SportZone.API/API/appsettings.Development.json
git diff --cached --stat
```

Đây là thay đổi Git cần review/commit. Vẫn phải làm sạch `appsettings.json` đang được publish. Việc viết lại lịch sử Git cần phối hợp với cộng tác viên; rotate credential là bước cần làm ngay dù có viết lại lịch sử hay không.

### 1.3. Thêm .dockerignore

Tạo `SportZone.API/.dockerignore` vì Docker build context trong hướng dẫn là `SportZone.API`:

```dockerignore
**/bin
**/obj
**/node_modules
**/.git
**/.vs
**/.vscode
**/appsettings.Development.json
**/.env
**/.env.*
**/secrets.json
**/*.pfx
**/*.key
```

Đừng ignore `API/wwwroot` vì đó là frontend cần đưa vào image.

### 1.4. Sửa URL App Runner cũ trong thanh toán

File `SportZone.API/API/Controllers/PaymentController.cs` hiện có URL App Runner viết trực tiếp cho callback và các redirect kết quả. Chỉ đổi `VnPay:ReturnUrl` trong cấu hình sẽ không đủ vì controller đang truyền URL hardcode vào service.

Cách sửa phù hợp với FE/BE chung domain:

1. Inject `IConfiguration` vào constructor hiện tại, lưu trong trường `_configuration`.
2. Đổi dòng khai báo `returnUrl` thành:

```csharp
var returnUrl = _configuration["VnPay:ReturnUrl"]
    ?? throw new InvalidOperationException("VnPay:ReturnUrl is missing");
```

3. Giữ lệnh gọi service với `returnUrl` vừa đọc.
4. Đổi các redirect về frontend thành đường dẫn tương đối cùng origin:

```csharp
return Redirect($"/payment-fail/{cleanId}");
// Nhánh không tìm thấy order:
return Redirect($"/payment-fail/{orderId}");
// Nhánh thanh toán thành công:
return Redirect($"/checkout-success/{orderId}");
```

Các dòng trên thuộc các nhánh khác nhau trong controller, không dán liên tiếp vào một vị trí. Khi có domain, cấu hình `VnPay__ReturnUrl=https://DOMAIN/api/payments/callback`. Cập nhật cấu hình merchant VNPay nếu nhà cung cấp yêu cầu đăng ký URL. Dùng credential và BaseUrl cùng môi trường sandbox hoặc live.

Tìm các URL cũ khác trong source, tránh tìm trong appsettings có secrets:

```powershell
rg -n 'awsapprunner\.com|localhost' SportZone.API SportZone.Client/src -g '*.cs' -g '*.ts' -g '!**/bin/**' -g '!**/obj/**'
```

### 1.5. Health check và HTTPS sau ALB

Thêm endpoint health đơn giản sau `var app = builder.Build();` và **trước `app.UseHttpsRedirection()`**:

```csharp
app.Map("/health", healthApp =>
{
    healthApp.Run(async context =>
    {
        context.Response.StatusCode = 200;
        context.Response.ContentType = "text/plain";
        await context.Response.WriteAsync("Healthy");
    });
});
```

Nhánh này trả HTTP 200 cho ALB mà không redirect sang HTTPS. Đây là liveness check: xác nhận tiến trình HTTP đang phục vụ, chưa chứng minh DB hoặc thanh toán hoạt động. Phải kiểm tra API thực tế ở bước cuối.

Trong ECS sẽ đặt `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` để app nhận biết HTTPS gốc từ ALB. Cờ này tin forwarded headers rộng, nên chỉ dùng khi security group ECS giới hạn inbound từ ALB như tài liệu. Cấu hình lâu dài nên dùng `ForwardedHeadersOptions` với các mạng proxy tin cậy và `UseForwardedHeaders()` trước HTTPS redirect.

### 1.6. Migration và seed cần kiểm tra trước khi mở cho người dùng

`Program.cs` hiện chạy `MigrateAsync` và seed khi khởi động, nhưng bắt lỗi rồi vẫn tiếp tục chạy ứng dụng. Vì vậy task healthy có thể vẫn đang lỗi DB.

Đối với lượt triển khai đầu lên DB mới, chỉ chạy 1 task, kiểm tra log migration/seed và kiểm tra API đọc DB. Kiểm tra seed không tạo tài khoản có mật khẩu mặc định hoặc ghi đè dữ liệu cần giữ.

Trước khi chạy production có nhiều replica/rolling deployments, tách migration thành job/one-off task trong cùng VPC rồi mới cập nhật service, hoặc có cơ chế khóa/điều phối đã kiểm chứng. `desired count = 1` vẫn có thể tạo hai task trong rolling deployment; không coi đây là giải pháp lâu dài cho migration.

Source dùng .NET 9. Theo chính sách tại ngày viết, phiên bản này hết hỗ trợ 10/11/2026; lên kế hoạch nâng .NET 10 LTS, gồm packages tương thích và kiểm thử, trước mốc đó. Không chỉ đổi tag Docker sang 10 trong khi project vẫn target net9.0.

## 2. Tạo VPC và subnets

AWS Console → chọn region `ap-southeast-1` → VPC → Create VPC.

1. Chọn **VPC and more**.
2. Name: `sportzone`.
3. IPv4 CIDR: ví dụ `10.20.0.0/16`, không trùng mạng cần kết nối khác.
4. Availability Zones: 2.
5. Public subnets: 2.
6. Private subnets: 2, tối thiểu để RDS có DB subnet group trải hai AZ.
7. NAT gateways = **None**. ECS sẽ dùng public subnets.
8. VPC endpoints = **None** cho quy trình này; private subnets chỉ dành cho RDS.
9. Bật DNS resolution và DNS hostnames.
10. Create VPC và đợi hoàn tất.

Kiểm tra Subnets → chọn từng subnet → Route table:

- Public: route `0.0.0.0/0` đi Internet Gateway (`igw-...`).
- Private dành cho RDS: giữ route local trong VPC, không cần default route ra Internet.
- RDS không có public IP; ECS truy cập RDS bằng route local của VPC.

Ghi lại VPC ID và phân biệt rõ hai public subnet, hai private subnet.

## 3. Tạo ba security groups

VPC → Security groups → Create security group. Cả ba chọn **cùng sportzone VPC**.

### 3.1. sportzone-alb-sg

Inbound: HTTP TCP 80 và HTTPS TCP 443, source `0.0.0.0/0` để phục vụ website public IPv4. Hướng dẫn dùng IPv4; nếu bật dualstack phải cấu hình IPv6 tương ứng.

### 3.2. sportzone-ecs-sg

Inbound: Custom TCP 8080, source chọn **security group ID của sportzone-alb-sg**. Không nhập IP của ALB vì IP có thể thay đổi. Không mở 8080 từ Internet.

### 3.3. sportzone-rds-sg

Inbound: PostgreSQL TCP 5432, source chọn **security group ID của sportzone-ecs-sg**.

Ban đầu có thể giữ outbound mặc định để kết nối DB, ECR, Secrets Manager, CloudWatch, Cloudinary và SMTP. Sau khi xác định nhu cầu, giới hạn outbound phù hợp; nếu sửa outbound ALB, phải cho phép cổng 8080 tới ECS SG.

Điều kiện để tiếp tục: đã có chuỗi ALB SG → ECS SG:8080 → RDS SG:5432.

## 4. Tạo RDS PostgreSQL mới

### 4.1. DB subnet group

RDS → Subnet groups → Create DB subnet group:

1. Name: `sportzone-db-subnets`.
2. Chọn sportzone VPC.
3. Chọn hai AZ và hai private subnets đã tạo.
4. Create.

### 4.2. Tạo instance mới và database sportzone

RDS → Databases → Create database:

1. Standard create → PostgreSQL.
2. Chọn phiên bản PostgreSQL đang được RDS hỗ trợ và tương thích với Npgsql/EF Core của ứng dụng. Đây là instance mới, không chọn Restore from snapshot.
3. Template Dev/Test cho môi trường thử; production chọn cấu hình độ sẵn sàng phù hợp.
4. DB identifier: `sportzone-db`.
5. Master username: ví dụ `sportzoneadmin`.
6. Tạo password mạnh hoặc dùng cơ chế RDS-managed secret. Lưu an toàn, không commit.
7. Chọn instance/storage theo nhu cầu; demo dùng instance nhỏ mà console hỗ trợ. Không giả định có Free Tier.
8. VPC: sportzone VPC; subnet group: `sportzone-db-subnets`.
9. Public access: **No**.
10. Existing VPC security group: chỉ `sportzone-rds-sg`, bỏ default nếu không cần.
11. Additional configuration → Initial database name: **sportzone**. DB identifier không phải database name.
12. Port: 5432. Bật encryption và automated backups; production bật deletion protection.
13. Create database, đợi status Available.
14. Copy endpoint hostname từ Connectivity & security.

### 4.3. Tài khoản ứng dụng và TLS

Production nên dùng user ứng dụng riêng với quyền cần thiết. User chạy migration cần DDL; user chỉ phục vụ request có thể hạn chế hơn. Hangfire dùng cùng DB và có nhu cầu tạo/cập nhật schema, cần tính vào quyền.

Để chạy SQL tạo user/import dump với RDS private, dùng máy quản trị có đường vào VPC như VPN/SSM tunnel/bastion được quản lý hoặc one-off task. Laptop bên ngoài không thể kết nối trực tiếp chỉ bằng việc mở SG.

Nếu đang triển khai demo lần đầu và chưa có kênh quản trị, có thể dùng master user để khởi tạo, rồi chuyển sang user ứng dụng khi thiết lập được kênh quản trị. Không giữ tài khoản master làm credential runtime production.

Connection string Npgsql ví dụ, nhập trực tiếp vào Secrets Manager:

```text
Host=RDS_ENDPOINT;Port=5432;Database=sportzone;Username=APP_USER;Password=DB_PASSWORD;SSL Mode=VerifyFull;Root Certificate=/app/certs/rds-global-bundle.pem
```

`VerifyFull` kiểm tra cả CA và hostname; phải đóng gói CA bundle chính thức RDS vào `/app/certs/rds-global-bundle.pem`. Có thể tải bundle vào `SportZone.API/API/certs` từ nguồn AWS ở phần tham khảo rồi cấu hình `.csproj` copy file này vào output/publish:

```xml
<ItemGroup>
  <None Update="certs/rds-global-bundle.pem"
        CopyToOutputDirectory="PreserveNewest"
        CopyToPublishDirectory="PreserveNewest" />
</ItemGroup>
```

Chứng chỉ CA public không phải secret. Kiểm tra file có trong image trước deploy. Nếu dùng `SSL Mode=Require` để kiểm tra kết nối ban đầu, hiểu rằng nó không thay thế việc kiểm tra danh tính server bằng `VerifyFull`; không sửa lỗi certificate bằng cách tắt TLS.

Password chứa ký tự đặc biệt của connection string như dấu chấm phẩy cần được quote/escape theo Npgsql; tránh ghép chuỗi tùy tiện gây lỗi parser.

## 5. Tạo Secrets Manager secret

Secrets Manager → Store a new secret:

1. Secret type: **Other type of secret**.
2. Chọn Key/value pairs.
3. Nhập mỗi tên sau thành một key riêng, value là giá trị thật tương ứng:

```text
ConnectionStrings__DefaultConnection = connection string RDS đầy đủ
TokenKey = JWT key ngẫu nhiên mạnh
VnPay__HashSecret = VNPay hash secret
VnPay__TmnCode = merchant code
CloudinarySettings__CloudName = cloud name hiện có
CloudinarySettings__ApiKey = API key
CloudinarySettings__ApiSecret = API secret đã rotate
EmailSettings__Email = SMTP account
EmailSettings__Password = SMTP/app password
```

4. Dùng encryption key mặc định `aws/secretsmanager` nếu không có yêu cầu KMS riêng.
5. Next → Secret name: **sportzone/prod**.
6. Chưa bật automatic rotation cho secret tổng hợp này. Rotation nhiều nhà cung cấp cần workflow riêng; đổi secret không tự đổi password bên dịch vụ.
7. Store, mở secret và copy **full ARN**, gồm cả suffix ngẫu nhiên.

Ví dụ ARN minh họa:

```text
arn:aws:secretsmanager:ap-southeast-1:123456789012:secret:sportzone/prod-AbCdEf
```

Đây chỉ là mẫu, không copy ARN này vào tài khoản của bạn.

Không đưa cả JSON secret vào một env var rồi kỳ vọng ASP.NET tự tách các key. Bước ECS sẽ map từng JSON key riêng.

Nếu RDS tự quản lý master secret, lưu ý secret đó thường chứa username/password chứ không phải connection string hoàn chỉnh. Copy một password vào secret app sẽ không tự đồng bộ khi RDS rotate. Dùng app user riêng hoặc thiết kế cơ chế cập nhật/redeploy rõ ràng.

## 6. Tạo IAM execution role và log group

### 6.1. Execution role

IAM → Roles → Create role:

1. Trusted entity: AWS service.
2. Use case: Elastic Container Service → Elastic Container Service Task.
3. Attach policy `AmazonECSTaskExecutionRolePolicy`.
4. Name: `sportzoneEcsExecutionRole`.
5. Trust relationship phải cho principal `ecs-tasks.amazonaws.com` gọi `sts:AssumeRole`.
6. Add permissions → Create inline policy → JSON:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": "secretsmanager:GetSecretValue",
      "Resource": "REPLACE_WITH_FULL_SECRET_ARN"
    }
  ]
}
```

Thay placeholder bằng ARN vừa copy, đặt tên policy `ReadSportzoneSecret`. Nếu dùng customer-managed KMS key, thêm quyền `kms:Decrypt` trên đúng key và bảo đảm key policy cho phép.

Execution role là role ECS dùng để pull image, đưa log và inject secret. Task role là quyền cho code gọi AWS. Giữ Cloudinary thì ứng dụng chưa cần quyền S3; nếu sau này dùng S3, cấp quyền ở **task role**.

### 6.2. Logs

CloudWatch → Logs → Log groups → Create log group:

- Name: `/ecs/sportzone-api`.
- Retention: ví dụ 14 ngày cho demo, chỉnh theo yêu cầu vận hành.
- Region phải khớp ECS.

Tạo log group trước để không phụ thuộc quyền tự tạo log group của execution role.

## 7. Build frontend và Docker image

Dockerfile hiện tại dùng .NET 9, context là `SportZone.API`, và chưa tự build Angular. Vì vậy mỗi release phải chạy Angular build trước.

```powershell
cd D:\ADMIN\Documents\Project\SportZone\SportZone.Client
npm ci
if ($LASTEXITCODE -ne 0) { throw 'npm ci failed' }
npm run build -- --configuration production
if ($LASTEXITCODE -ne 0) { throw 'Angular build failed' }

cd D:\ADMIN\Documents\Project\SportZone
Test-Path .\SportZone.API\API\wwwroot\index.html
docker build --pull --platform linux/amd64 -f .\SportZone.API\Dockerfile -t sportzone-api:v1 .\SportZone.API
if ($LASTEXITCODE -ne 0) { throw 'Docker build failed' }
```

`Test-Path` phải trả True. Angular build có thể thay đổi/xóa file build cũ trong `wwwroot`; lưu các thay đổi đang làm trước khi build.

Chọn X86_64 trong ECS để khớp `linux/amd64`. Dockerfile đang dùng port 8080; không cấu hình ALB target port 80 cho container này.

Kiểm tra artifact mà không chạy ứng dụng và không cần secrets:

```powershell
docker run --rm --entrypoint sh sportzone-api:v1 -c 'test -f /app/wwwroot/index.html && test ! -f /app/appsettings.Development.json && test -f /app/certs/rds-global-bundle.pem'
if ($LASTEXITCODE -ne 0) { throw 'Image is missing required files or contains development config' }
```

Lệnh trên không xác nhận appsettings.json đã sạch. Bạn phải review file đó trước build. Không in toàn bộ environment/connection string vào logs để kiểm tra.

Chạy app local cần DB truy cập được và secrets riêng; không bắt buộc mở RDS public chỉ để thử local. Kiểm tra runtime cloud ở bước 11.

## 8. Tạo ECR và push image

Trong PowerShell đã đăng nhập AWS:

```powershell
$DeployRegion = 'ap-southeast-1'
$DeployAccount = (aws sts get-caller-identity --query Account --output text).Trim()
if ($LASTEXITCODE -ne 0) { throw 'AWS login failed' }
$DeployRegistry = "$DeployAccount.dkr.ecr.$DeployRegion.amazonaws.com"
$DeployTag = 'v1'

aws ecr create-repository --repository-name sportzone-api --region $DeployRegion --image-tag-mutability IMMUTABLE --image-scanning-configuration scanOnPush=true
```

Chỉ tạo repository một lần. Nếu đã có repository cùng tên, kiểm tra đúng repository rồi dùng nó; không xóa để tạo lại.

```powershell
aws ecr get-login-password --region $DeployRegion | docker login --username AWS --password-stdin $DeployRegistry
if ($LASTEXITCODE -ne 0) { throw 'ECR login failed' }
docker tag sportzone-api:v1 "$DeployRegistry/sportzone-api:$DeployTag"
docker push "$DeployRegistry/sportzone-api:$DeployTag"
if ($LASTEXITCODE -ne 0) { throw 'Image push failed' }
```

ECR Console → sportzone-api → Images phải xuất hiện tag `v1`. Copy Image URI, có dạng `ACCOUNT.dkr.ecr.ap-southeast-1.amazonaws.com/sportzone-api:v1`. Những release tiếp theo dùng tag mới, không ghi đè `v1`.

## 9. Tạo ALB và HTTPS

### 9.1. Target group

EC2 Console → Target Groups → Create target group:

1. Target type: **IP addresses**.
2. Name: `sportzone-web-tg`.
3. Protocol HTTP, port **8080**, IP address type IPv4.
4. VPC: sportzone VPC.
5. Health check protocol HTTP, path **/health**.
6. Success code **200**. Không dùng 200–399 để che lỗi redirect health check.
7. Chưa đăng ký IP thủ công. ECS sẽ đăng ký task.

### 9.2. ALB

EC2 → Load Balancers → Create → Application Load Balancer:

1. Name `sportzone-alb`.
2. Scheme **Internet-facing**, IPv4.
3. Chọn sportzone VPC và hai **public subnet ở hai AZ**.
4. Security group: `sportzone-alb-sg`.
5. Có thể tạo listener HTTP 80 forward vào target group để kiểm tra hạ tầng ngắn hạn; không dùng HTTP cho đăng nhập/thanh toán thực.
6. Create và đợi Active.

### 9.3. Domain và certificate

Để dùng HTTPS trên ALB, cần domain bạn kiểm soát. Không thể xin certificate ACM cho hostname `*.elb.amazonaws.com` thuộc AWS.

1. ACM cùng region ALB → Request public certificate.
2. Nhập domain, ví dụ `shop.example.com`.
3. DNS validation → tạo CNAME validation ở nơi quản lý DNS, giữ record để certificate tự gia hạn.
4. Đợi certificate Issued.
5. ALB → Listeners → Add listener HTTPS 443 → certificate ACM vừa tạo → Forward `sportzone-web-tg`.
6. Sửa listener 80: Redirect sang HTTPS 443, status 301.
7. Route 53: tạo A Alias domain → ALB. Nếu DNS ở nhà cung cấp khác, subdomain có thể dùng CNAME trỏ ALB DNS; apex domain tùy khả năng ALIAS/ANAME của nhà cung cấp.

Chưa có domain thì có thể kiểm tra tạm ALB DNS/health. Chưa hoàn thành HTTPS production và chưa kiểm thử đăng nhập/thanh toán bằng dữ liệu thật.

## 10. Tạo ECS cluster và task definition

### 10.1. Cluster

ECS → Clusters → Create cluster → name `sportzone-cluster` → chọn hạ tầng Fargate/serverless nếu console hiển thị lựa chọn → Create. Không cần tạo EC2 container instances.

### 10.2. Task definition

ECS → Task definitions → Create new task definition:

1. Family `sportzone-api`.
2. Launch type/capacity: Fargate.
3. OS Linux, architecture **X86_64**.
4. Network mode **awsvpc**.
5. CPU **0.5 vCPU**, memory **1 GB** làm điểm bắt đầu; tăng nếu log cho thấy thiếu RAM/CPU.
6. Task execution role: `sportzoneEcsExecutionRole`.
7. Task role: không cấp quyền AWS không cần thiết. Nếu console yêu cầu role, tạo role ECS task riêng với trust đúng và không có policy S3 khi chưa dùng S3.
8. Container name `sportzone-web`, essential bật.
9. Image URI: URI ECR tag `v1`.
10. Port mapping: container port **8080**, TCP, app protocol HTTP nếu có.

Environment variables thường:

```text
ASPNETCORE_ENVIRONMENT = Production
ASPNETCORE_URLS = http://+:8080
ASPNETCORE_FORWARDEDHEADERS_ENABLED = true
VnPay__ReturnUrl = https://DOMAIN_CUA_BAN/api/payments/callback
EmailSettings__Host = SMTP_HOST_DANG_SU_DUNG
EmailSettings__Port = SMTP_PORT_DANG_SU_DUNG
EmailSettings__DisplayName = SportZone
```

SMTP host/port dùng theo nhà cung cấp và cách TLS đang dùng trong EmailService; không mặc định mọi nhà cung cấp đều dùng cùng cổng. Giữ các giá trị VNPay không nhạy cảm như BaseUrl, Version, Command, CurrCode, Locale trong appsettings.json hoặc thêm env tương ứng. Không bật Development chỉ để có Swagger/Hangfire dashboard ngoài Internet.

### 10.3. Map secrets đúng cách

Trong container → Environment variables/Secrets → chọn loại **ValueFrom** hoặc **Secrets Manager**, tùy giao diện console. Mỗi env dùng full ARN có suffix key:

```text
ENV NAME: ConnectionStrings__DefaultConnection
VALUE FROM: FULL_SECRET_ARN:ConnectionStrings__DefaultConnection::

ENV NAME: TokenKey
VALUE FROM: FULL_SECRET_ARN:TokenKey::

ENV NAME: VnPay__HashSecret
VALUE FROM: FULL_SECRET_ARN:VnPay__HashSecret::

ENV NAME: VnPay__TmnCode
VALUE FROM: FULL_SECRET_ARN:VnPay__TmnCode::

ENV NAME: CloudinarySettings__CloudName
VALUE FROM: FULL_SECRET_ARN:CloudinarySettings__CloudName::

ENV NAME: CloudinarySettings__ApiKey
VALUE FROM: FULL_SECRET_ARN:CloudinarySettings__ApiKey::

ENV NAME: CloudinarySettings__ApiSecret
VALUE FROM: FULL_SECRET_ARN:CloudinarySettings__ApiSecret::

ENV NAME: EmailSettings__Email
VALUE FROM: FULL_SECRET_ARN:EmailSettings__Email::

ENV NAME: EmailSettings__Password
VALUE FROM: FULL_SECRET_ARN:EmailSettings__Password::
```

Thay `FULL_SECRET_ARN` bằng ARN thật. Hai dấu `::` cuối để trống version stage/id và dùng bản mặc định hiện tại. Không thêm dấu nháy vào giá trị ARN trong UI.

Nếu dùng JSON editor, cấu trúc của từng entry trong `containerDefinitions[].secrets` như sau:

```json
{
  "name": "TokenKey",
  "valueFrom": "arn:aws:secretsmanager:ap-southeast-1:123456789012:secret:sportzone/prod-AbCdEf:TokenKey::"
}
```

Không điền secret value vào `environment` dạng plaintext. Dùng Fargate Linux platform **1.4.0 trở lên**, hoặc LATEST đáp ứng yêu cầu này, để inject JSON key.

### 10.4. Logging

Logging driver: `awslogs`.

```text
awslogs-group = /ecs/sportzone-api
awslogs-region = ap-southeast-1
awslogs-stream-prefix = ecs
```

Lưu task definition. Chưa thêm container health command dùng curl nếu image chưa có curl; ALB health check đủ cho hướng dẫn này.

## 11. Tạo service và kiểm tra lần đầu

ECS → sportzone-cluster → Services → Create:

1. Compute/launch type: Fargate, dùng capacity thông thường cho lần đầu.
2. Task definition family `sportzone-api`, chọn revision vừa lưu.
3. Service name `sportzone-service`, desired tasks **1**.
4. VPC sportzone.
5. Chọn hai **public subnets** có route `0.0.0.0/0` tới Internet Gateway.
6. Đặt **Assign public IP = Enabled**. Không chọn private subnets cho ECS trong kiến trúc này.
7. Security group chỉ `sportzone-ecs-sg`.
8. Load balancing: Application Load Balancer → existing `sportzone-alb` → target group `sportzone-web-tg` → container `sportzone-web:8080`.
9. Health check grace period: ví dụ 180 giây để app khởi động/migration lần đầu. Nếu migration dài, xử lý thành job riêng thay vì tăng vô hạn.
10. Bật deployment circuit breaker + rollback nếu console hỗ trợ.
11. Create service.

Kiểm tra theo đúng thứ tự:

1. Tasks: PROVISIONING/PENDING chuyển RUNNING.
2. CloudWatch Logs có dòng app listen trên 8080; không có lỗi migration/seeding/DB.
3. Target group → Targets chuyển Healthy.
4. ECS Events không lặp dừng/chạy task.
5. Mở `https://DOMAIN/health` trả Healthy.
6. Mở trang chủ, tải lại một route Angular con để kiểm tra fallback.
7. Trong DevTools → Network, request API đi về cùng domain `/api/...`, không về App Runner cũ hoặc localhost.
8. Gọi chức năng đọc sản phẩm/category để xác nhận DB hoạt động. Health check 200 không thay thế bước này.

Lệnh kiểm tra sau khi domain/certificate đã sẵn sàng:

```powershell
$DeployDomain = 'shop.example.com'
Invoke-WebRequest "https://$DeployDomain/health"
Invoke-WebRequest "https://$DeployDomain/"
```

Thay domain ví dụ bằng domain thật. Trước khi DNS hoạt động, kiểm tra ALB target health trong console.

## 12. Kiểm thử chức năng trước khi công bố

- Đăng ký/đăng nhập và gọi API cần JWT.
- Xem sản phẩm, giỏ hàng, tạo đơn thử bằng dữ liệu thử.
- Upload ảnh lên Cloudinary, xem ảnh và xóa ảnh thử.
- Gửi email, kiểm tra cả inbox/spam và log SMTP.
- VNPay sandbox: thanh toán thành công, hủy/thất bại, callback trở về đúng domain.
- Kiểm tra job Hangfire chạy, đặc biệt các job liên quan đơn hàng.
- Refresh route như trang chi tiết không trả 404 từ server.
- Kiểm tra API lỗi trả dữ liệu lỗi phù hợp, không nhầm HTML trang Angular là API thành công.
- Kiểm tra log không ghi password, token hoặc toàn bộ environment.

Nếu sử dụng Identity token/email reset giữa nhiều task hoặc qua restart, kiểm tra chiến lược lưu Data Protection keys; key chỉ ở container có thể làm token trước đó không dùng được. JWT TokenKey là một loại key khác, không tự giải quyết Data Protection.

Với DB mới, xác nhận migrations đã tạo bảng và seed đã thêm dữ liệu khởi tạo. Kiểm tra đường dẫn ảnh Cloudinary trong dữ liệu seed. Tạo tài khoản quản trị theo cơ chế của ứng dụng; không giả định migration đã tự tạo admin.

## 13. Deploy phiên bản tiếp theo và rollback

### 13.1. Release mới

1. Hoàn tất/review thay đổi source và secret-free config.
2. Build Angular production trước mỗi lần Docker build.
3. Build/tag/push image bằng tag mới như `v2` hoặc commit SHA.
4. ECR phải có image mới trước khi cập nhật ECS.
5. Nếu có thay đổi schema, backup và chạy migration theo quy trình đã tách/điều phối.
6. Task definitions → Create new revision → đổi image URI sang tag mới.
7. Service → Update → chọn revision mới → Deploy.
8. Chờ ổn định, kiểm tra logs/health và luồng người dùng.

Nếu chỉ đổi giá trị Secrets Manager: sửa secret, sau đó ECS Service → Update → **Force new deployment**. Task cũ không tự nhận secret mới. Đổi DB password phải phối hợp cập nhật DB và secret để tránh gián đoạn.

### 13.2. Rollback

Service → Update → chọn task definition revision đã chạy tốt → Deploy. Giữ lại image tương ứng trong ECR.

Rollback container không tự rollback database. Schema migration phải tương thích với phiên bản cũ hoặc có kế hoạch phục hồi riêng. Circuit breaker lần deploy đầu chưa có deployment tốt trước đó để quay lại; vẫn cần đọc stopped reason.

## 14. Tra cứu lỗi thường gặp

### Task dừng ngay, ResourceInitializationError

ECS → Tasks → lọc Stopped → mở task → đọc Stopped reason và container reason. Kiểm tra secret ARN/key, execution role GetSecretValue, KMS nếu có và đường mạng tới Secrets Manager/CloudWatch.

### CannotPullContainerError

Kiểm tra ECR region/URI/tag, quyền execution role, kiến trúc image. ECS phải nằm trong public subnet, public IP phải Enabled và subnet có route IGW. Kiểm tra outbound SG/NACL cho phép kết nối. Chỉ có route IGW nhưng task không được cấp public IP thì task vẫn không truy cập Internet IPv4 được.

### ALB báo Unhealthy hoặc 502/503

Kiểm tra app listen 8080, target group type IP, ECS SG nhận 8080 từ ALB SG, `/health` trả đúng 200 và app không crash. Đừng sửa success codes thành mọi status để task trông có vẻ healthy.

### Redirect loop/too many redirects

Kiểm tra forwarded headers, `X-Forwarded-Proto`, env Production và HTTPS listener. TLS kết thúc ở ALB nhưng Kestrel nhận HTTP là bình thường; app phải nhận biết scheme gốc.

### Lỗi DB timeout

Kiểm tra endpoint/port, VPC, RDS SG nguồn ECS SG, RDS Available, route và DNS. Với public ECS và private RDS trong cùng VPC, kết nối DB vẫn dùng mạng private, không cần RDS public.

### Password authentication failed / database does not exist

Kiểm tra user/password thật, secret key mapping, tên database `sportzone`, và force deployment sau khi đổi secret. DB identifier `sportzone-db` không phải tên database trong connection string.

### Lỗi certificate PostgreSQL

Kiểm tra CA bundle có trong image đúng `/app/certs/rds-global-bundle.pem`, hostname là endpoint RDS và bundle chính thức còn phù hợp. Không tắt xác minh chỉ để bỏ qua lỗi production.

### Task healthy nhưng trang không có dữ liệu

Kiểm tra log `Lỗi trong quá trình Seeding dữ liệu` và lỗi migration. Code hiện bắt lỗi startup nên tiến trình vẫn có thể phục vụ HTML/health khi DB chưa sẵn sàng.

### VNPay quay về App Runner cũ

Kiểm tra đã sửa hardcode ở PaymentController, đã build image mới và service đang chạy revision mới. Chỉ đổi secret/env ReturnUrl không ảnh hưởng dòng URL hardcode chưa sửa.

### Swagger/Hangfire dashboard không mở

Code chỉ bật chúng trong Development. Production không có dashboard public là hành vi hiện tại. Dùng CloudWatch để chẩn đoán; muốn dashboard production cần thiết kế xác thực và giới hạn truy cập riêng.

### Ảnh hoặc email timeout

Kiểm tra public IP của task, route Internet Gateway, outbound SG/NACL, Cloudinary credential, SMTP port/TLS và chính sách nhà cung cấp. Tránh phụ thuộc SMTP port 25; dùng cổng submission/TLS nhà cung cấp hỗ trợ.

## 15. Khi nào chuyển frontend/ảnh sang S3 và CloudFront?

Giữ Cloudinary cho lần deploy này vì PhotoService đã dùng Cloudinary và lưu PublicId. Không lưu upload lâu dài trong filesystem container.

Nếu chuyển ảnh sang S3: viết implementation IPhotoService mới, cấp S3 permissions theo bucket/prefix cho task role, giữ bucket private, phân phối ảnh qua CloudFront hoặc presigned URLs phù hợp. Cần migration URL/object key; không chỉ thay một API key.

Nếu tách Angular: build ra thư mục riêng, upload static files vào S3 private, đặt CloudFront với Origin Access Control. API vẫn ở ALB/ECS. Khi dùng domain API riêng, đổi apiUrl và CORS allowlist; khi dùng cùng CloudFront route `/api/*` tới ALB, chuyển đủ methods/query/auth headers và tắt cache API nhạy cảm. SPA route fallback chỉ áp dụng frontend, tránh biến lỗi API thành HTML 200.

## 16. Checklist kết thúc

- [ ] Đã tạo RDS mới và database `sportzone`; không restore/import DB cũ.
- [ ] Đã rotate secrets cũ và làm sạch file cấu hình được publish.
- [ ] Đã sửa URL VNPay App Runner hardcode.
- [ ] Image có Angular, CA bundle; không có development config.
- [ ] Secret mapping dùng đúng key `__` và đúng execution role.
- [ ] Không tạo NAT Gateway; ECS ở public subnet, public IP Enabled và có route Internet Gateway.
- [ ] RDS private, chỉ nhận truy cập từ ECS SG.
- [ ] ALB health 200, HTTPS/domain hoạt động.
- [ ] API đọc/ghi DB, login, ảnh, email và thanh toán sandbox đã kiểm tra.
- [ ] Có backup, log retention, budget và kế hoạch rollback.
- [ ] Có kế hoạch nâng .NET trước khi hết hỗ trợ.

## Tài liệu chính thức để đối chiếu

- [ECS inject Secrets Manager JSON keys và yêu cầu platform](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/secrets-envvar-secrets-manager.html)
- [ECS execution role và quyền secrets/KMS](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/task_execution_IAM_role.html)
- [Fargate networking](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/fargate-task-networking.html)
- [Fargate task requirements](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/fargate-tasks-services.html)
- [ECS và ALB](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/alb.html)
- [ECR build/authenticate/push](https://docs.aws.amazon.com/AmazonECR/latest/userguide/getting-started-cli.html)
- [RDS PostgreSQL SSL/TLS](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/PostgreSQL.Concepts.General.SSL.html)
- [RDS CA bundle chính thức](https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem)
- [ACM certificate cho ALB](https://docs.aws.amazon.com/elasticloadbalancing/latest/application/https-listener-certificates.html)
- [ASP.NET Core environment configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-9.0)
- [ASP.NET Core forwarded headers](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-9.0)
- [.NET support lifecycle](https://dotnet.microsoft.com/en-us/platform/support/policy)
