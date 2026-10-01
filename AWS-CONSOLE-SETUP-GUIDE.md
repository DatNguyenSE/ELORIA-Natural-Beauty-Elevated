# SportZone — cấu hình từng bước trên AWS Console

Region: **Asia Pacific (Singapore), `ap-southeast-1`**. Ngày đối chiếu tài liệu: 30/09/2026.

File này hướng dẫn tạo tài nguyên, lấy các thông số và kết nối chúng trên AWS Console. Phần sửa source, build Docker và xử lý lỗi chi tiết nằm trong [AWS-DEPLOYMENT-GUIDE.md](./AWS-DEPLOYMENT-GUIDE.md). Các tài nguyên bên dưới chưa được tạo tự động. Tên nhóm/trường trong console có thể thay đổi nhẹ; đối chiếu theo tên dịch vụ và chức năng.

## BẮT ĐẦU Ở ĐÂY — làm đúng thứ tự từ trên xuống

**Chưa tạo tài nguyên nào thì bắt đầu ở bước 1 dưới đây. Không vào EC2 tạo ALB trước. Không cần tạo EC2 instance.**

Các mục 1–2 phía dưới giải thích kiến trúc và loại thông số cần lấy. Quy trình thao tác thực tế là danh sách sau; mỗi liên kết mở đúng phần cần làm.

1. **Đăng nhập AWS, chọn Singapore và đặt cảnh báo chi phí.** Làm [mục 3](#console-account). Xong khi đã chọn `ap-southeast-1` và tạo budget.
2. **Tạo VPC trước.** Làm [mục 4](#console-vpc): tạo 1 VPC, 2 public subnets, 2 private subnets, Internet Gateway; chọn NAT = None và VPC endpoints = None. Xong khi public subnets có route tới IGW và bạn đã ghi lại các IDs.
3. **Tạo security groups theo thứ tự ALB → ECS → RDS.** Làm [mục 5](#console-security-groups). Phải tạo ALB SG trước để chọn nó làm source của ECS SG, rồi chọn ECS SG làm source của RDS SG. Đây mới là tạo nhóm mạng, chưa tạo load balancer.
4. **Tạo DB subnet group rồi tạo RDS mới.** Làm [mục 6](#console-rds). Chọn private subnets, Public access = No, database name = `sportzone`. Đợi Available, lấy endpoint/port và lưu credential.
5. **Tạo Secrets Manager secret.** Làm [mục 7](#console-secrets). Dùng endpoint RDS vừa lấy để điền connection string, thêm các secret khác rồi copy full Secret ARN.
6. **Tạo IAM execution role rồi CloudWatch log group.** Làm [mục 8](#console-iam-logs). Policy của role dùng Secret ARN ở bước 5. Xong khi có `sportzoneEcsExecutionRole` và `/ecs/sportzone-api`.
7. **Tạo ECR repository, chuẩn bị code, build và push image.** Làm [mục 9](#console-ecr). Việc tạo repository làm trên Console; việc build/push chạy trên máy theo hướng dẫn được dẫn tại đó. Chỉ tiếp tục khi tab Images có image và bạn đã copy URI có tag. Image phải có frontend, `/health`, CA bundle và không chứa secret cũ.
8. **Tạo target group trước, rồi tạo ALB.** Làm [mục 10](#console-alb). Lúc này mới vào EC2 → Load Balancers → Create → Application Load Balancer. Target group dùng IP/8080, ALB dùng hai public subnets và ALB SG đã tạo. Đợi ALB Active; chưa có task nên target group còn trống là bình thường.
9. **Tạo ECS cluster rồi task definition.** Làm [mục 11](#console-ecs-task). Điền image URI bước 7, execution role/log group bước 6 và secret references bước 5. Lưu revision, chưa cần chạy task thủ công.
10. **Tạo ECS service để chạy ứng dụng.** Làm [mục 12](#console-ecs-service). Chọn public subnets, Assign public IP = Enabled, ECS SG; gắn ALB/target group bước 8, desired tasks = 1. Đợi task RUNNING và target Healthy, kiểm tra logs và DB.
11. **Gắn domain và HTTPS.** Làm [mục 13](#console-https): xin ACM certificate, xác thực DNS, tạo listener 443 rồi đổi listener 80 sang redirect, trỏ domain về ALB. Cần domain bạn kiểm soát. Không nhập thông tin đăng nhập/thanh toán thật qua HTTP ở giai đoạn kiểm tra trước đó.
12. **Cập nhật URL thật và kiểm tra cuối.** Làm [mục 14](#console-update) để đặt `VnPay__ReturnUrl` theo domain thật và deploy revision mới nếu cần. Sau đó kiểm tra [checklist mục 17](#console-finish).

**Ngay bây giờ, nếu bạn đang ở trang “Compare and select load balancer type” mà chưa có VPC/subnets/security groups: quay về ô tìm kiếm AWS, nhập VPC và làm bước 2.**

Thông số lấy ở bước trước sẽ được dùng ở bước sau. Không cần tạo Access Key riêng cho RDS/ECS/ECR/ALB; phần “key” được giải thích ở mục 2.

## 1. Phương án cố định của dự án

**Không dùng NAT Gateway.** ALB và ECS task ở public subnet; ECS bật public IP. RDS ở private subnet. ECS security group chỉ nhận cổng 8080 từ ALB.

- Người dùng truy cập domain/ALB, không dùng public IP của task làm địa chỉ website.
- ECS dùng public IP và route Internet Gateway để truy cập ECR, Secrets Manager, CloudWatch, Cloudinary và SMTP.
- Không cần tạo VPC endpoints cho quy trình này.
- ECS kết nối RDS qua mạng private cùng VPC.
- Chạy 1 ECS task lúc bắt đầu; không cấu hình autoscaling trong hướng dẫn này.
- Tạo RDS mới và database `sportzone` mới, không restore/import DB cũ.
- Angular và ASP.NET Core chung một image; giữ Cloudinary cho ảnh.

ALB, Fargate, RDS, public IPv4, logs và Secrets Manager có thể tính phí. Không có chi phí NAT Gateway trong kiến trúc này. Budget là cảnh báo, không tự chặn chi tiêu.

## 2. Hiểu đúng “key” cần lấy từ từng dịch vụ

Bạn không cần tạo một AWS Access Key cho từng service. Các giá trị sẽ dùng là:

- **RDS:** endpoint hostname, port, database name, username và password. Password là secret; endpoint không phải secret.
- **Secrets Manager:** full Secret ARN để ECS tham chiếu. Secret ARN không phải nội dung password.
- **ECR:** repository URI và image URI có tag/digest. Không có mật khẩu ECR cố định để chép vào appsettings.
- **ECS:** cluster name, service name, task definition family/revision và execution role ARN để quản lý deploy.
- **ALB:** DNS name, target group ARN, listener và certificate ARN nếu dùng HTTPS. ALB không cấp API key cho ứng dụng.
- **IAM:** role cung cấp quyền cho ECS. Không nhét AWS access key/secret access key vào container để đọc Secrets Manager.

Cloudinary API key/API secret, VNPay HashSecret và SMTP password lấy từ nhà cung cấp tương ứng rồi lưu vào Secrets Manager. AWS không tự sinh những credential này cho bạn.

<a id="console-account"></a>

## 3. Đăng nhập, chọn region và tạo cảnh báo chi phí

1. Đăng nhập [AWS Console](https://console.aws.amazon.com/) bằng tài khoản/role được cấp quyền triển khai.
2. Góc trên bên phải → Region → **Asia Pacific (Singapore)**.
3. Kiểm tra tên tài khoản/Account ID đúng tài khoản của bạn.
4. Các dịch vụ tài nguyên trong hướng dẫn phải cùng `ap-southeast-1`. IAM và Billing là các dịch vụ toàn cục nên không hiển thị region giống RDS/ECS.
5. Ô tìm kiếm → **Billing and Cost Management** → **Budgets** → **Create budget**.
6. Chọn Cost budget theo tháng hoặc template tương ứng.
7. Nhập ngân sách USD theo mức bạn chấp nhận, ví dụ 30 USD chỉ là ngưỡng cảnh báo, không phải ước tính chi phí hệ thống.
8. Nhập email nhận cảnh báo; cấu hình mức actual/forecast theo nhu cầu.
9. Create budget. Nếu tài khoản đang dùng không có quyền Billing, nhờ quản trị tài khoản cấp quyền phù hợp.

<a id="console-vpc"></a>

## 4. Tạo VPC, 2 public subnet và 2 private subnet

Mở [VPC Console tại Singapore](https://ap-southeast-1.console.aws.amazon.com/vpc/home?region=ap-southeast-1).

1. Your VPCs → **Create VPC**.
2. Resources to create → **VPC and more**.
3. Name tag auto-generation → nhập `sportzone`.
4. IPv4 CIDR → `10.20.0.0/16`, nếu không trùng mạng cần kết nối khác.
5. IPv6 → No IPv6 CIDR block cho hướng dẫn IPv4 này.
6. Number of Availability Zones → **2**.
7. Number of public subnets → **2**.
8. Number of private subnets → **2**.
9. NAT gateways → **None**.
10. VPC endpoints → **None**. Không để wizard tự tạo S3 endpoint nếu mục tiêu là làm đúng cấu hình này.
11. Bật DNS hostnames và DNS resolution.
12. Xem preview: có Internet Gateway, không có NAT Gateway.
13. Create VPC → đợi workflow hoàn tất.

### 4.1. Lấy VPC ID và subnet IDs

Your VPCs → chọn VPC có Name `sportzone-vpc` hoặc tên wizard sinh → copy **VPC ID** dạng `vpc-...`.

Subnets → lọc theo VPC ID đó. Ghi lại:

```text
PUBLIC_SUBNET_A_ID  = subnet-...
PUBLIC_SUBNET_B_ID  = subnet-...
PRIVATE_SUBNET_A_ID = subnet-...
PRIVATE_SUBNET_B_ID = subnet-...
```

Hai public subnets phải ở hai AZ khác nhau. Hai private subnets cũng trải hai AZ để tạo DB subnet group.

### 4.2. Kiểm tra route trước khi tiếp tục

Subnets → chọn public subnet → tab Route table:

- Có route local `10.20.0.0/16`.
- Có `0.0.0.0/0` → `igw-...`.

Chọn private subnet:

- Có route local của VPC.
- Không cần `0.0.0.0/0` ra Internet.

Việc subnet có route IGW không tự cấp public IP cho Fargate. Bạn vẫn phải bật **Assign public IP** khi tạo ECS service ở bước 12.

<a id="console-security-groups"></a>

## 5. Tạo security groups theo đúng nguồn truy cập

VPC → Security groups → **Create security group**. Cả ba phải dùng sportzone VPC vừa tạo.

### 5.1. Group cho ALB

- Name: `sportzone-alb-sg`.
- Description: `Public HTTP HTTPS to SportZone ALB`.
- Inbound → Add rule → HTTP, TCP 80, source Anywhere-IPv4 `0.0.0.0/0`.
- Add rule → HTTPS, TCP 443, source Anywhere-IPv4 `0.0.0.0/0`.
- Outbound: giữ mặc định cho lần cấu hình đầu.
- Create → copy **Security group ID** dạng `sg-...`.

### 5.2. Group cho ECS

- Name: `sportzone-ecs-sg`.
- Description: `Only ALB can reach container port 8080`.
- Inbound → Custom TCP, port 8080.
- Source → Custom → tìm/chọn **ID của sportzone-alb-sg**.
- Không chọn Anywhere-IPv4 cho 8080.
- Outbound: giữ mặc định để gọi AWS services, Cloudinary, SMTP và RDS.
- Create → copy group ID.

### 5.3. Group cho RDS  sg-0f81f53c00293c1a5

- Name: `sportzone-rds-sg`.
- Description: `PostgreSQL from ECS tasks`.
- Inbound → PostgreSQL, TCP 5432.
- Source → Custom → chọn **ID của sportzone-ecs-sg**.
- Create → copy group ID.

Kiểm tra bằng tên và ID, đừng chọn nhầm default SG. Đừng dùng public IP của ECS làm source cho RDS: task kết nối DB bằng địa chỉ private, và IP của task có thể đổi.

<a id="console-rds"></a>

## 6. Tạo RDS PostgreSQL mới và lấy connection information

Mở [RDS Console](https://ap-southeast-1.console.aws.amazon.com/rds/home?region=ap-southeast-1).

### 6.1. Tạo DB subnet group

1. Subnet groups → **Create DB subnet group**.
2. Name: `sportzone-db-subnets`.
3. Description: `Private subnets for SportZone PostgreSQL`.
4. VPC: sportzone VPC.
5. Add subnets → chọn hai AZ và hai **private subnet IDs** đã lưu.
6. Create.

### 6.2. Tạo DB instance

1. Databases → **Create database**.
2. Creation method → **Standard create** để nhìn được cấu hình mạng.
3. Engine → **PostgreSQL**, không chọn Aurora nếu làm theo hướng dẫn này.
4. Chọn engine version được RDS hỗ trợ và phù hợp ứng dụng.
5. Template → Dev/Test nếu môi trường thử nghiệm; chọn production settings phù hợp khi vận hành thực tế.
6. Availability → Single-AZ cho lần chạy thử tiết kiệm; đây không phải cấu hình DB có dự phòng đa AZ.
7. DB instance identifier → `sportzone-db`.
8. Master username → `sportzoneadmin` hoặc tên bạn chọn, ghi lại.
9. Credentials management: để đi theo luồng nhập password rõ ràng, chọn **Self managed**, tạo password mạnh và lưu vào password manager. Nếu chọn RDS-managed password thì xem lưu ý bên dưới.
10. Instance class: chọn loại nhỏ được hỗ trợ cho thử nghiệm. Xem estimated cost, không suy luận rằng template là miễn phí.
11. Storage: chọn dung lượng/loại hợp nhu cầu; nếu bật autoscaling, kiểm tra giới hạn dung lượng tối đa.
12. Connectivity → không tự connect to EC2; chọn sportzone VPC thủ công.
13. DB subnet group → `sportzone-db-subnets`.
14. Public access → **No**.
15. VPC security group → Choose existing → chỉ chọn `sportzone-rds-sg`.
16. Database port → **5432**.
17. Database authentication → password authentication cho luồng này.
18. Additional configuration → **Initial database name: `sportzone`**.
19. Bật encryption, automated backups theo nhu cầu; chọn retention phù hợp, ví dụ 7 ngày. Production nên bật deletion protection.
20. Review chi phí và cấu hình → **Create database**.
21. Đợi status **Available**.

`sportzone-db` là tên instance AWS; `sportzone` là tên database PostgreSQL. Không dùng hai tên thay thế lẫn nhau trong connection string.

### 6.3. Lấy endpoint, port, username và password

Databases → `sportzone-db`:

1. Tab **Connectivity & security** → Endpoint & port → copy **Endpoint** và **Port**.
2. Endpoint có dạng `sportzone-db....ap-southeast-1.rds.amazonaws.com`. Không có `https://` và không thêm `/`.


3. Tab **Configuration** → kiểm tra Master username và DB name nếu console hiển thị.
4. Password self-managed là giá trị bạn vừa đặt. **Console không cho đọc lại password này**. Nếu quên, Modify DB instance để đặt password mới rồi cập nhật nơi sử dụng; không có nút lấy password cũ.
5. Nếu chọn RDS-managed credentials, dùng liên kết master credentials secret trong thông tin DB để mở Secrets Manager, rồi Retrieve secret value với quyền phù hợp.

RDS-managed master secret có thể rotate. Việc copy password từ đó sang secret khác không tạo đồng bộ tự động; không bật rotation rồi giữ một connection string cũ mà không có quy trình cập nhật.

### 6.4. Giá trị đưa vào connection string

Ví dụ chỉ gồm placeholder:

```text
Host=RDS_ENDPOINT;Port=5432;Database=sportzone;Username=APP_DB_USER;Password=APP_DB_PASSWORD;SSL Mode=VerifyFull;Root Certificate=/app/certs/rds-global-bundle.pem
```

Host=sportzone-db.cvu08u0yajy5.ap-southeast-1.rds.amazonaws.com;Port=5432;Database=sportzone;Username=sportzoneadmin;Password=Dat6112005nt!;SSL Mode=VerifyFull;Root Certificate=/app/certs/rds-global-bundle.pem




Chuỗi này cần CA bundle có trong Docker image như hướng dẫn chính ở bước 4.3. Không điền VerifyFull mà quên đóng gói file CA.

Production nên tạo user ứng dụng riêng. RDS Console tạo master user, không phải một giao diện SQL tổng quát để tạo mọi PostgreSQL user. Với RDS private, cần kênh quản trị trong VPC/VPN/SSM tunnel hoặc one-off task để tạo app user. Demo có thể khởi tạo bằng master tạm thời rồi chuyển sang app user; không gọi master user là tài khoản hạn chế quyền.

DB mới chưa có bảng ứng dụng. Migration/seed của source sẽ khởi tạo khi app chạy; log phải được kiểm tra vì code hiện bắt lỗi migration rồi vẫn có thể tiếp tục chạy.

<a id="console-secrets"></a>

## 7. Tạo Secrets Manager và lấy ARN/key references

Mở [Secrets Manager Console](https://ap-southeast-1.console.aws.amazon.com/secretsmanager/home?region=ap-southeast-1).

### 7.1. Nhập secret

1. **Store a new secret**.
2. Secret type → **Other type of secret**.
3. Chọn Key/value pairs.
4. Thêm từng cặp, tên key nhập đúng như dưới đây, value nhập credential thật:

```text
ConnectionStrings__DefaultConnection
TokenKey
VnPay__HashSecret
VnPay__TmnCode
CloudinarySettings__CloudName
CloudinarySettings__ApiKey
CloudinarySettings__ApiSecret
EmailSettings__Email
EmailSettings__Password
```

`TokenKey` phải là signing key ngẫu nhiên mạnh do bạn tạo, không phải AWS Access Key. `ConnectionStrings__DefaultConnection` chứa cả connection string ở bước 6.4.

5. Encryption key → `aws/secretsmanager` mặc định nếu không có yêu cầu KMS riêng.
6. Next → Secret name → **sportzone/prod**.
7. Description → `Production configuration for SportZone ECS`.
8. Automatic rotation → chưa bật cho secret tổng hợp này; rotation DB/SMTP/Cloudinary cần phối hợp với từng dịch vụ.
9. Next → Review → **Store**.

### 7.2. Lấy full Secret ARN

Secrets → chọn `sportzone/prod` → phần Secret details → copy **Secret ARN**.

```text
arn:aws:secretsmanager:ap-southeast-1:ACCOUNT_ID:secret:sportzone/prod-RANDOM
```

Copy nguyên ARN thật, kể cả suffix ngẫu nhiên. Không tự gõ lại ARN từ tên secret.

Để kiểm tra giá trị: Secret value → **Retrieve secret value**. Đây là dữ liệu nhạy cảm; không copy ra repository hoặc chia sẻ ảnh chụp hiển thị secret.

### 7.3. Tạo chuỗi tham chiếu cho ECS

Với mỗi key, ECS dùng:

```text
FULL_SECRET_ARN:JSON_KEY::
```

Ví dụ với JWT:

```text
arn:aws:secretsmanager:ap-southeast-1:ACCOUNT_ID:secret:sportzone/prod-RANDOM:TokenKey::
```

`FULL_SECRET_ARN` và ví dụ ở đây phải được thay bằng ARN thật. Hai dấu `::` cuối để trống version stage/id. Không dùng ARN của cả secret cho từng biến nếu bạn muốn chỉ lấy một JSON value.

<a id="console-iam-logs"></a>

## 8. Tạo IAM role cho ECS và CloudWatch log group

### 8.1. Execution role

1. Ô tìm kiếm → IAM → Roles → **Create role**.
2. Trusted entity type → AWS service.
3. Service/use case → Elastic Container Service → **Elastic Container Service Task**.
4. Permissions → chọn **AmazonECSTaskExecutionRolePolicy**.
5. Role name → **sportzoneEcsExecutionRole** → Create role.
6. Mở role → Permissions → Add permissions → **Create inline policy** → JSON.
7. Dán policy dưới đây và thay ARN placeholder bằng full ARN thật từ bước 7:

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

8. Policy name → `ReadSportzoneProductionSecret` → Create.
9. Tab Trust relationships phải chứa service principal `ecs-tasks.amazonaws.com` với action `sts:AssumeRole`.
10. Trang role → copy **ARN**, dùng cho Task execution role trong ECS.

Nếu dùng customer-managed KMS key, bổ sung `kms:Decrypt` trên đúng key và kiểm tra key policy. Với key mặc định ở bước 7, không thêm tùy tiện quyền `kms:*`.

Execution role phục vụ ECS pull image/inject secrets/gửi logs. Task role phục vụ code ứng dụng gọi AWS. Không đưa S3 permissions vào execution role rồi kỳ vọng PhotoService sẽ dùng được.

### 8.2. Log group

1. CloudWatch Console → kiểm tra region Singapore.
2. Logs → Log groups → **Create log group**.
3. Log group name → **/ecs/sportzone-api**.
4. Create.
5. Chọn group → chỉnh retention, ví dụ **14 days** cho môi trường thử.

<a id="console-ecr"></a>

## 9. Tạo ECR và lấy image URI

Mở [ECR Console](https://ap-southeast-1.console.aws.amazon.com/ecr/home?region=ap-southeast-1).

1. Private registry → Repositories → **Create repository**.
2. Visibility → Private nếu console có lựa chọn.
3. Repository name → **sportzone-api**.
4. Image tag mutability → Immutable để mỗi release dùng tag mới.
5. Giữ encryption mặc định hoặc theo yêu cầu tổ chức.
6. Bật/cấu hình image scanning trong repository hoặc registry settings nếu giao diện cung cấp.
7. Create repository.
8. Mở repository → copy **URI**:

```text
ACCOUNT_ID.dkr.ecr.ap-southeast-1.amazonaws.com/sportzone-api
```

### 9.1. Đẩy image từ máy lên ECR

ECR Console không có nút upload source code để build ứng dụng. Repository chỉ có thể chạy được sau khi bạn build và push Docker image.

Trong repository → **View push commands** → chọn hướng dẫn phù hợp. Chạy AWS CLI/Docker trên máy đã đăng nhập, không dán Access Key vào appsettings.

Dùng lệnh build Angular + Docker trong hướng dẫn chính, sau đó login/tag/push theo View push commands. Đảm bảo image đã làm sạch secrets, có frontend và `/health` trước khi deploy.

ECR auth token là credential tạm thời do CLI lấy, không phải key bạn cần lưu trong Secrets Manager. ECS pull image bằng execution role.

### 9.2. Lấy image URI có tag

Repository → Images → chọn image tag `v1` → **Copy URI** hoặc xem Image URI:

```text
ACCOUNT_ID.dkr.ecr.ap-southeast-1.amazonaws.com/sportzone-api:v1
```

Đây là giá trị dán vào container Image URI của ECS. Repository URI thiếu `:v1` không chỉ rõ bản release bạn vừa push. Có thể dùng digest `@sha256:...` khi muốn cố định chính xác image.

<a id="console-alb"></a>

## 10. Tạo target group và ALB

Mở [EC2 Console](https://ap-southeast-1.console.aws.amazon.com/ec2/home?region=ap-southeast-1). ALB nằm trong mục Load Balancing của EC2 Console dù bạn không chạy EC2 instance.

### 10.1. Target group

1. Load Balancing → Target Groups → **Create target group**.
2. Target type → **IP addresses**, không chọn Instances.
3. Name → **sportzone-web-tg**.
4. Protocol HTTP, port **8080**.
5. IP address type IPv4, VPC sportzone.
6. Protocol version HTTP1 cho ứng dụng hiện tại.
7. Health check → HTTP, path **/health**.
8. Advanced health check → Success codes **200**.
9. Next → chưa thêm IP thủ công → Create.
10. Mở target group → copy **Target group ARN**.

Điều kiện: source/image phải có `/health` trả HTTP 200 trước HTTPS redirect như hướng dẫn chính. ECS sẽ đăng ký IP private của task vào group này; public IP dùng cho outbound.

### 10.2. ALB

1. Load Balancers → **Create load balancer** → Application Load Balancer → Create.
2. Name → **sportzone-alb**.
3. Scheme → **Internet-facing**.
4. IP address type → IPv4.
5. Network mapping → sportzone VPC.
6. Chọn hai AZ, mỗi AZ chọn một **public subnet**.
7. Security groups → `sportzone-alb-sg`, bỏ default nếu không cần.
8. Listener HTTP 80 → Forward to `sportzone-web-tg` cho kiểm tra hạ tầng ban đầu.
9. Không cần thêm WAF/CloudFront/tích hợp khác chỉ để hoàn thành luồng này.
10. Review → Create load balancer → đợi **Active**.

### 10.3. Lấy địa chỉ truy cập

Load Balancers → sportzone-alb → Details → copy **DNS name**.

```text
sportzone-alb-....ap-southeast-1.elb.amazonaws.com
```

ALB DNS là hostname ổn định để trỏ domain; không lấy một IP resolve được của ALB rồi tạo bản ghi DNS cố định. ALB ARN dùng cho quản trị, không dán ARN vào trình duyệt.

Chưa có ECS task đăng ký nên target group trống/ALB trả 503 lúc này là có thể dự kiến. Chỉ kiểm thử ứng dụng sau bước 12.

<a id="console-ecs-task"></a>

## 11. Tạo ECS cluster và task definition

Mở [ECS Console](https://ap-southeast-1.console.aws.amazon.com/ecs/v2/clusters?region=ap-southeast-1).

### 11.1. Cluster

1. Clusters → **Create cluster**.
2. Name → **sportzone-cluster**.
3. Chọn Fargate/serverless nếu console hiển thị lựa chọn hạ tầng.
4. Không thêm EC2 capacity cho quy trình này.
5. Create → mở cluster và ghi lại name/Cluster ARN trong thông tin cluster nếu cần tự động hóa.

### 11.2. Task definition cơ bản

1. Task definitions → **Create new task definition** bằng form.
2. Family → **sportzone-api**.
3. Infrastructure/launch type → **AWS Fargate**.
4. OS Linux, CPU architecture **X86_64**, khớp image `linux/amd64`.
5. Network mode **awsvpc**.
6. Task CPU **0.5 vCPU**, memory **1 GB** làm cấu hình bắt đầu, theo dõi để điều chỉnh.
7. Task execution role → **sportzoneEcsExecutionRole**.
8. Task role → chưa cần quyền S3/Secrets Manager cho code vì giữ Cloudinary và secret được ECS inject. Nếu tổ chức yêu cầu task role, tạo role riêng với quyền tối thiểu.

### 11.3. Container

- Container name: **sportzone-web**.
- Image URI: image URI ECR có tag `v1` vừa copy.
- Essential container: Yes.
- Port mappings → container port **8080**, TCP, app protocol HTTP nếu có.
- Không thay entrypoint/command khi Dockerfile đã chạy `SportZone.API.dll`.

### 11.4. Environment variables thường

Trong Environment variables → Add, chọn **Value**, nhập các cặp:

```text
ASPNETCORE_ENVIRONMENT          = Production
ASPNETCORE_URLS                 = http://+:8080
ASPNETCORE_FORWARDEDHEADERS_ENABLED = true
VnPay__ReturnUrl                = https://DOMAIN_THAT/api/payments/callback
EmailSettings__Host            = SMTP_HOST_THAT
EmailSettings__Port            = SMTP_PORT_THAT
EmailSettings__DisplayName     = SportZone
```

Không dùng nguyên placeholder. Nếu chưa có domain, chưa kiểm thử VNPay thực tế; cập nhật ReturnUrl ở revision sau khi có HTTPS.

Các giá trị VNPay không nhạy cảm còn lại phải có trong appsettings.json hoặc env: BaseUrl, Version, Command, CurrCode, Locale. Production không tự đọc appsettings.Development.json.

Cờ forwarded headers ở đây yêu cầu giữ inbound ECS chỉ từ ALB. Xem hướng dẫn chính để cấu hình proxy tin cậy chặt chẽ hơn. Không bật Development để công khai Swagger/Hangfire dashboard.

### 11.5. Environment variables từ Secrets Manager

Mỗi dòng chọn **ValueFrom/Secret** tùy console, không chọn Value dạng plaintext. Điền Name như sau, ValueFrom là ARN ghép tương ứng:

```text
Name: ConnectionStrings__DefaultConnection
ValueFrom: FULL_SECRET_ARN:ConnectionStrings__DefaultConnection::

Name: TokenKey
ValueFrom: FULL_SECRET_ARN:TokenKey::

Name: VnPay__HashSecret
ValueFrom: FULL_SECRET_ARN:VnPay__HashSecret::

Name: VnPay__TmnCode
ValueFrom: FULL_SECRET_ARN:VnPay__TmnCode::

Name: CloudinarySettings__CloudName
ValueFrom: FULL_SECRET_ARN:CloudinarySettings__CloudName::

Name: CloudinarySettings__ApiKey
ValueFrom: FULL_SECRET_ARN:CloudinarySettings__ApiKey::

Name: CloudinarySettings__ApiSecret
ValueFrom: FULL_SECRET_ARN:CloudinarySettings__ApiSecret::

Name: EmailSettings__Email
ValueFrom: FULL_SECRET_ARN:EmailSettings__Email::

Name: EmailSettings__Password
ValueFrom: FULL_SECRET_ARN:EmailSettings__Password::
```

Thay FULL_SECRET_ARN trong từng dòng. Nếu UI chỉ hỗ trợ chọn secret nhưng không chọn JSON key, dùng task definition JSON editor để cấu hình `secrets` đúng cú pháp:

```json
"secrets": [
  {
    "name": "TokenKey",
    "valueFrom": "FULL_SECRET_ARN:TokenKey::"
  }
]
```

Đây là đoạn minh họa bên trong container definition, không phải toàn bộ task definition. Phải bổ sung tất cả các secret mappings cần thiết; không chỉ TokenKey.

### 11.6. Logging và lưu revision

Logging → bật Amazon CloudWatch/driver awslogs:

```text
awslogs-group         = /ecs/sportzone-api
awslogs-region        = ap-southeast-1
awslogs-stream-prefix = ecs
```

Create task definition. Trang chi tiết cho biết family/revision, ví dụ `sportzone-api:1`, và **Task definition ARN**. Ghi lại revision để chọn khi tạo service.

Không cấu hình curl container health check nếu image không có curl. ALB health check đã được cấu hình riêng.

<a id="console-ecs-service"></a>

## 12. Tạo ECS service: public subnet và bật public IP

1. Clusters → sportzone-cluster → Services → **Create**.
2. Compute → Fargate launch type hoặc capacity provider **FARGATE**; chưa dùng FARGATE_SPOT cho lần đầu.
3. Platform version → LATEST, bảo đảm Linux platform ít nhất 1.4.0 cho secret JSON key injection.
4. Deployment/application type → Service/Replica theo UI.
5. Task family `sportzone-api`, revision vừa tạo.
6. Service name **sportzone-service**.
7. Desired tasks **1**.
8. Networking → sportzone VPC.
9. Subnets → chỉ chọn hai **public subnet IDs**.
10. Security groups → chỉ **sportzone-ecs-sg**.
11. **Public IP / Assign public IP → Enabled**.
12. Load balancing → Application Load Balancer → Use existing `sportzone-alb`.
13. Chọn container **sportzone-web:8080** và existing target group **sportzone-web-tg**. Nếu form yêu cầu listener, chọn listener gắn target group này.
14. Health check grace period → ví dụ **180 seconds** để chờ khởi động lần đầu.
15. Deployment → rolling update; bật deployment circuit breaker/rollback nếu có.
16. Không bật service autoscaling trong lần đầu này.
17. Review → Create.

**Chốt ba trường quan trọng nhất:** public subnets + Assign public IP Enabled + ECS SG chỉ nhận 8080 từ ALB SG. Không chọn private subnet cho ECS vì kiến trúc này không có NAT.

### 12.1. Lấy thông tin task để kiểm tra

Cluster → service → Tasks → chọn task:

- Configuration/Networking cho biết subnet, ENI và IP. Task phải có public IPv4 để outbound Internet theo phương án này.
- Logs mở stream tương ứng trong CloudWatch.
- Container details hiển thị image/reason nếu container dừng.

Không dùng public IP task làm domain website hoặc VNPay ReturnUrl. ALB chuyển tiếp tới private IP task; ECS tự cập nhật target khi task thay đổi.

### 12.2. Xác nhận các dịch vụ đã nối đúng

1. ECS task chuyển RUNNING, service running count = 1.
2. CloudWatch Logs không có lỗi đọc secrets, DB, migration hoặc seed.
3. EC2 → Target groups → sportzone-web-tg → Targets có IP private task, port 8080, status Healthy.
4. ECS → Service → Events không lặp stop/start task.
5. Truy cập `/health` qua ALB/domain trả 200, không redirect bất thường.
6. API đọc sản phẩm hoạt động; trang HTML/health 200 chưa chứng minh DB hoạt động.

Trong rolling update, desired count 1 vẫn có thể tạm thời có hai task. Đọc phần migration trong hướng dẫn chính trước release có thay đổi schema.

<a id="console-https"></a>

## 13. HTTPS: ACM và domain

ALB DNS dùng để trỏ tên miền. Để có HTTPS với certificate hợp lệ trên ALB, cần domain bạn kiểm soát.

### 13.1. Certificate

1. Mở ACM tại **Singapore**, cùng region ALB.
2. Request certificate → Request public certificate.
3. Domain name: ví dụ `shop.example.com`, thay bằng domain thật.
4. Validation method: DNS validation.
5. Request → mở certificate.
6. Copy CNAME name/value trong Domain validation.
7. Tạo CNAME đó tại DNS provider. Nếu dùng Route 53 và có quyền, dùng Create records in Route 53.
8. Đợi status **Issued**; giữ DNS validation record để gia hạn.
9. Copy **Certificate ARN** nếu cần chọn thủ công.

Không xin certificate cho ALB hostname `*.elb.amazonaws.com` vì bạn không kiểm soát domain của AWS.

### 13.2. ALB listener 443

1. EC2 → Load Balancers → sportzone-alb → Listeners and rules → Add listener.
2. Protocol HTTPS, port 443.
3. Default action → Forward to sportzone-web-tg.
4. Chọn certificate ACM vừa Issued và security policy được console đề xuất phù hợp client.
5. Save.
6. Listener HTTP 80 → Edit default action → Redirect to HTTPS, port 443, status 301.

### 13.3. DNS website

Route 53 → Hosted zones → domain → Create record:

- Record name: `shop` nếu website là `shop.example.com`.
- Type A, Alias bật.
- Route traffic to: Alias to Application and Classic Load Balancer.
- Region: Singapore.
- Chọn sportzone-alb → Create record.

Nếu DNS ở nơi khác, subdomain dùng CNAME tới ALB DNS; apex domain cần ALIAS/ANAME hoặc giải pháp tương ứng của nhà cung cấp. Không tạo hosted zone mới rồi kỳ vọng có hiệu lực nếu nameserver domain chưa trỏ vào hosted zone đó.

Đợi DNS/certificate hoạt động → mở `https://DOMAIN/health` và trang chủ. Đăng nhập/thanh toán thực chỉ kiểm thử sau khi HTTPS hoàn tất.

<a id="console-update"></a>

## 14. Cập nhật giá trị sau khi cấu hình xong

### 14.1. Đổi secret value

Secrets Manager → sportzone/prod → Retrieve secret value → Edit → sửa đúng key → Save. Sau đó ECS → service → Update → **Force new deployment**.

Task đang chạy không tự nạp secret mới. Nếu đổi DB password, cập nhật cả DB và secret theo kế hoạch để tránh task mới dùng password không khớp.

### 14.2. Đổi URL/domain hoặc image

Task definitions → sportzone-api → Create new revision → sửa env/image → Create. Service → Update → chọn revision mới → Deploy.

Với source hiện tại, URL App Runner trong PaymentController phải được sửa trước khi build image. Chỉ đổi `VnPay__ReturnUrl` ở Console không thắng được URL hardcode đang được controller truyền vào service.

### 14.3. Credential nào không nên “lấy rồi dán”?

- Không lấy AWS access keys để điền vào appsettings cho ECS.
- Không lưu ECR login token vào Secrets Manager.
- Không dùng Secret ARN như password của DB.
- Không dùng ALB ARN thay hostname website.
- Không dùng master DB password lâu dài cho app production khi đã có thể tạo user ứng dụng riêng.

## 15. Mẫu ghi chú các giá trị đã tạo

Bạn có thể copy mẫu này vào ghi chú riêng. Chỉ lưu identifiers, không điền password/API secret vào file được commit:

```text
AWS_ACCOUNT_ID=
AWS_REGION=ap-southeast-1
VPC_ID=
PUBLIC_SUBNET_A_ID=
PUBLIC_SUBNET_B_ID=
PRIVATE_SUBNET_A_ID=
PRIVATE_SUBNET_B_ID=
ALB_SECURITY_GROUP_ID=
ECS_SECURITY_GROUP_ID=
RDS_SECURITY_GROUP_ID=
RDS_ENDPOINT=
RDS_PORT=5432
RDS_DATABASE=sportzone
RDS_APP_USERNAME=
APP_SECRET_ARN=
ECS_EXECUTION_ROLE_ARN=
ECR_REPOSITORY_URI=
ECR_IMAGE_URI=
ECS_CLUSTER_NAME=sportzone-cluster
ECS_SERVICE_NAME=sportzone-service
ECS_TASK_DEFINITION_REVISION=
CLOUDWATCH_LOG_GROUP=/ecs/sportzone-api
TARGET_GROUP_ARN=
ALB_DNS_NAME=
ACM_CERTIFICATE_ARN=
WEBSITE_DOMAIN=
VNPAY_RETURN_URL=
```

## 16. Khi gặp lỗi, mở màn hình nào trước?

- **Task không chạy:** ECS → Cluster → Tasks → lọc Stopped → Stopped reason và container reason.
- **CannotPullContainerError:** kiểm tra image URI/tag trong ECR, execution role, public IP và route IGW.
- **ResourceInitializationError:** kiểm tra ARN/key Secrets Manager, IAM policy, log group và outbound network.
- **ALB 503:** EC2 → Target group → Targets; xem đã có target và Healthy chưa.
- **Target Unhealthy:** kiểm tra 8080, `/health`, ECS SG source ALB SG, log app.
- **DB timeout:** RDS Available, endpoint đúng, RDS SG source ECS SG, cùng VPC.
- **DB authentication failed:** username/password/database name và task đã redeploy sau đổi secret chưa.
- **Trang lên nhưng API lỗi:** CloudWatch log migration/seed, không chỉ nhìn health check.
- **SMTP/Cloudinary timeout:** task public IP, route IGW, outbound và credential nhà cung cấp.
- **HTTPS redirect loop:** forwarded headers và listener; xem phần source trong hướng dẫn chính.

<a id="console-finish"></a>

## 17. Checklist trước khi kết thúc

- [ ] Tất cả tài nguyên theo region Singapore, trừ IAM/Billing toàn cục.
- [ ] Không tạo NAT Gateway hoặc VPC endpoints.
- [ ] Public subnets có route IGW; private subnets dành cho RDS.
- [ ] ECS public IP Enabled, chỉ nhận 8080 từ ALB SG.
- [ ] RDS tạo mới, Public access No, DB name sportzone.
- [ ] Secret ARN/key references đúng, không có password plaintext trong task definition.
- [ ] Image đã push ECR, execution role đủ quyền pull/log/secret.
- [ ] Target type IP, container port 8080, health /health trả 200.
- [ ] Domain/HTTPS hoạt động, API và DB được kiểm thử thực tế.
- [ ] Có budget, log retention và backup cho DB mới.

## Nguồn chính thức

- [Tạo RDS DB instance](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_CreateDBInstance.html)
- [RDS PostgreSQL SSL/TLS](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/PostgreSQL.Concepts.General.SSL.html)
- [Lấy secret value trên Console](https://docs.aws.amazon.com/secretsmanager/latest/userguide/retrieving-secrets-console.html)
- [Cấu trúc Secret ARN và giá trị](https://docs.aws.amazon.com/secretsmanager/latest/userguide/whats-in-a-secret.html)
- [Inject Secrets Manager vào ECS](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/secrets-envvar-secrets-manager.html)
- [ECS execution role](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/task_execution_IAM_role.html)
- [Fargate networking/public IP](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/fargate-task-networking.html)
- [ECR image push](https://docs.aws.amazon.com/AmazonECR/latest/userguide/getting-started-cli.html)
- [ALB cho ECS](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/alb.html)
- [ALB HTTPS listener](https://docs.aws.amazon.com/elasticloadbalancing/latest/application/create-https-listener.html)
