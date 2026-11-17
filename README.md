# Ecount Manager

영업팀이 전화 응대 중 Ecount ERP의 고객, 판매, 채권, 품목, 가격 및 재고 정보를 빠르게 확인할 수 있도록 만든 C# 데스크톱 통합 뷰어입니다.

현재 버전은 **공식 Open API 기반 읽기 전용 MVP**입니다. 주문 등록과 전표 수정은 Ecount 웹페이지에서 수행하며, 앱에는 공식적으로 조회 가능한 품목·재고 정보와 로컬 상담 메모만 표시합니다.

## 현재 구현

- 테스트/운영 서버 선택, Zone 자동조회 및 로그인 세션 발급
- Ecount 공식 품목 조회 API 연동
- 전체 재고 및 창고별 재고 조회 API 연동
- 품목코드, 품목명, 규격, 바코드, 창고명 검색
- API 인증키를 파일에 저장하지 않는 세션 전용 입력 방식
- 항상 위에 표시되는 상담 메모 위젯
- 상호명, 전화번호, 문의내용 및 수정되지 않는 최초 생성시간 저장

## 실행

```powershell
dotnet run --project .\src\EcountManager\EcountManager.csproj
```

## 구조

- `Models`: Ecount 조회 데이터와 상담 메모 모델
- `Services`: Ecount Open API 클라이언트와 로컬 메모 저장소
- `ViewModels`: 검색, 필터, 화면 상태를 관리하는 MVVM 계층
- `MainWindow.xaml`: 공식 API 품목·재고 뷰어
- `MemoWidgetWindow.xaml`: 데스크톱 펫형 상담 메모 위젯
- `docs/PRODUCTION_PROCESS.md`: 순차 제작 계획과 완료 기준

## 실제 Ecount 연결

1. Ecount의 `Self-Customizing > 정보관리 > API인증키발급`에서 테스트 인증키를 준비합니다.
2. 앱 왼쪽에서 회사코드, 인증키 발급 ID, API 인증키를 입력합니다.
3. 테스트 서버를 선택한 상태로 `연결하고 동기화`를 실행합니다.
4. 메뉴별 검증 후 정식 인증키를 발급받으면 테스트 서버 선택을 해제하고 운영 인증키로 연결합니다.

API 인증키는 앱을 종료하면 사라지며 Git 저장소나 설정 파일에 기록되지 않습니다.

