# Ecount Manager

영업팀이 전화 응대 중 Ecount ERP의 고객, 판매, 채권, 품목, 가격 및 재고 정보를 빠르게 확인할 수 있도록 만든 C# 데스크톱 통합 뷰어입니다.

현재 버전은 **읽기 전용 MVP**입니다. 주문 등록과 전표 수정은 Ecount 웹페이지에서 수행하며, 이 앱은 상담에 필요한 정보를 한 화면에 모으는 데 집중합니다.

## 현재 구현

- 상호명, 전화번호, 고객코드 기반 고객 검색
- 고객 기본정보 및 채권 요약
- 판매 이력의 품목, 규격, 금액, 담당자 통합 검색
- 신규 주문 상담을 위한 품목별 현재고와 가격 비교
- 채권 발생 및 입금 이력 조회
- Ecount API 연결 전 UI 검증을 위한 샘플 데이터 서비스
- API 구현체 교체가 가능한 서비스 인터페이스

## 실행

```powershell
dotnet run --project .\src\EcountManager\EcountManager.csproj
```

## 구조

- `Models`: 화면과 API 계층이 공유하는 조회 모델
- `Services`: Ecount 조회 계약과 현재 샘플 데이터 구현체
- `ViewModels`: 검색, 필터, 화면 상태를 관리하는 MVVM 계층
- `MainWindow.xaml`: 영업 상담용 통합 대시보드
- `docs/PRODUCTION_PROCESS.md`: 순차 제작 계획과 완료 기준

