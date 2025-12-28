import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'pages/login_page.dart';
import 'pages/home_page.dart';
import 'pages/ticket/ticket_list_page.dart';
import 'pages/ticket/ticket_detail_page.dart';
import 'pages/ticket/create_ticket_page.dart';
import 'pages/ticket/missing_info_questionnaire_page.dart';
import 'providers/auth_provider.dart';
import 'providers/ticket_provider.dart';

void main() {
  runApp(const MyApp());
}

class MyApp extends StatelessWidget {
  const MyApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => AuthProvider()),
        ChangeNotifierProvider(create: (_) => TicketProvider()),
      ],
      child: MaterialApp(
        title: '现场问题反馈系统',
        theme: ThemeData(
          colorScheme: ColorScheme.fromSeed(
            seedColor: const Color(0xFF667eea),
          ),
          useMaterial3: true,
          appBarTheme: const AppBarTheme(
            centerTitle: true,
            elevation: 0,
          ),
          cardTheme: CardTheme(
            elevation: 2,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
          ),
          elevatedButtonTheme: ElevatedButtonThemeData(
            style: ElevatedButton.styleFrom(
              padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(8),
              ),
            ),
          ),
        ),
        initialRoute: '/',
        routes: {
          '/': (context) => const LoginPage(),
          '/home': (context) => const HomePage(),
          '/tickets': (context) => const TicketListPage(),
          '/tickets/create': (context) => const CreateTicketPage(),
        },
        onGenerateRoute: (settings) {
          // 动态路由
          if (settings.name?.startsWith('/tickets/') ?? false) {
            final uri = Uri.parse(settings.name!);
            final ticketId = uri.pathSegments.last;

            if (uri.pathSegments.contains('missing-info')) {
              return MaterialPageRoute(
                builder: (context) => MissingInfoQuestionnairePage(ticketId: ticketId),
              );
            }

            return MaterialPageRoute(
              builder: (context) => TicketDetailPage(ticketId: ticketId),
            );
          }

          return null;
        },
        debugShowCheckedModeBanner: false,
      ),
    );
  }
}
