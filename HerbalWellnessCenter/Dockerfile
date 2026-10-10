# ==============================================================================
# GIAI ĐOẠN 1: BUILD VÀ PUBLISH ỨNG DỤNG (SDK 8.0)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sao chép file dự án để khôi phục các gói thư viện NuGet (Tối ưu hóa Cache của Docker)
COPY ["HerbalWellnessCenter.csproj", "./"]
RUN dotnet restore "HerbalWellnessCenter.csproj"

# Sao chép toàn bộ mã nguồn còn lại của dự án
COPY . .
WORKDIR "/src/."

# Tiến hành Build và Publish ở chế độ Release, tối ưu hóa tối đa hiệu suất thực thi
RUN dotnet publish "HerbalWellnessCenter.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==============================================================================
# GIAI ĐOẠN 2: RUNTIME CHÍNH THỨC (ASP.NET CORE 8.0 RUNTIME - SIÊU GỌN BẢO MẬT)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Thiết lập múi giờ chuẩn Việt Nam (ICT - Indochina Time) phục vụ đặt lịch hẹn chính xác
ENV TZ=Asia/Ho_Chi_Minh

# Cấu hình biến môi trường lắng nghe cổng mạng chuẩn cho Cloud
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Sao chép kết quả đã publish từ Giai đoạn 1 sang môi trường chạy thực tế
COPY --from=build /app/publish .

# Cấu hình lệnh khởi chạy ứng dụng mặc định
ENTRYPOINT ["dotnet", "HerbalWellnessCenter.dll"]