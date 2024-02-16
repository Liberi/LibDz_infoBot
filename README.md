# Version 2.4 (Release)
Всем привет! 👋 Это [Telegram бот](https://t.me/LibDz_infoBot) колледжа ВКЭ, он сделан для получения разной информации (в частности ДЗ и текущем положении дел и т.п) связанной с колледжем.
Бот разрабатывался/разрабатывается как экспериментальный проект пользователем [@Lib_int](https://t.me/Lib_int).

![image](https://github.com/Liberi/LibDz_infoBot/assets/130091860/c549c2b9-fcf4-40e4-aee9-d66b1ec498f8)


> [!IMPORTANT]
> Если вы захотите поэкспериментировать с моим кодом, пожалуйста <sup> **не запускайте** </sup> его со стандартным API токеном!

## Необходимые компоненты
> [!NOTE]
> Для корректной работы бота требуется [**.NET 6.0**](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-6.0.418-windows-x64-installer) + <sub>_(при нажатии на ссылку установка сразу начнется!)_</sub> [Visual C++](https://aka.ms/vs/16/release/VC_redist.x64.exe) _(Для установки бд)_ и любая версия [**SQL Server LocalDB**](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)!

### Для запуска Бд необходимо: 
_Если уже есть рабочая версия Sql Server, работа "из коробки" с большей вероятностью возможна._
1) Локальная/или другая Бд
2) Проверка ее наличия (Далее команды Cmd)
   - `SqlLocalDb i` или сразу `SqlLocalDb i MSSQLLocalDB`
   - `MSSQLLocalDB` т.к данное имя базы по стандарту указано в [App.config](https://github.com/Liberi/LibDz_infoBot/blob/main/App.config)
3) При ее отсутствии: `SqlLocalDb create "MSSQLLocalDB`
4) При проверке состояния через `SqlLocalDb i MSSQLLocalDB`, при значении `State: Stopped`
   - Выполнить `SqlLocalDb s MSSQLLocalDB`
5) Далее необходимо проверить доступность файла базы `icacls DatabaseTelegramBot.mdf`
   - При отсутсвии разрешений на на запись **`(W)`**, а лучше полный доступ **`(F)`** необходимо выдать соответсвующие разрешения:
   - Первым делом необходимо перейти в папку с базами `.\LibDz_infoBot\Databases`
   - Для основного файла Бд `icacls DatabaseTelegramBot.mdf /grant Имя_Юзера:(F)`
   - Для файлов логов `icacls DatabaseTelegramBot_log.ldf /grant Имя_Юзера:(F)`

![image](https://github.com/Liberi/LibDz_infoBot/assets/130091860/4cd73bf6-cba9-451a-a37f-86be9720784b)

7) При успешном выполнении команд и запуске Локальной базы дальнейший запуск бота дожен происходить штатно.

![image](https://github.com/Liberi/LibDz_infoBot/assets/130091860/91a33add-32d7-4908-917e-60a657f795d7)

## Актуальные команды:
> [!NOTE]
> Естественно данные команды работают в запущенном боте, в его чате Telegram, при этом для общения поддерживаются только личные чаты.
- **/start** ➯ Запуск бота
- **/help** ➯ Помощь и основная информация
- **/group** ➯ Изменение группы
- **/profil**e ➯ Информация о вас в нашей базе
- **/menu** ➯ Переход в главное меню
- **/cancel** ➯ Отмена выполнения предыдущего действия
- **/news** ➯ Отобразить последние новости
- **/fast_edit_dz** ➯ Быстрый переход к редактированию дз
- **/change_admin_type** ➯ Изменение типа администратора
- **/update_photo_news** ➯ Обновить фото к новости
- **/update_week_type** ➯ Обновить тип недели
- **/chat_send_dz** ➯ Отправить вручную дз в ваш чат
- **/update_id_group** ➯ Обновить Id вашего чата дз
- **/update_id_admin** ➯ Обновить Id Админа для получения уведомлений
- **/update_send_dz** ➯ Обновить возможность отправки дз в ваш чат
- **/restart_bot** ➯ Перезагрузить бота администратору
> [!TIP]
> Если вы хотите добавить бота в свой канал, вам нужно выдать ему права администратора. При этом он не будет отвечать на сообщения, а только присылать ДЗ в определенное время.
## Список ошибок:
- **[0000]** ➱ Выключение или блокировка сервера;

- `**[1_00]**` Exeption
  - **[1100]** ➱ Не удалось обработать текст;
  - **[1200]** ➱ Не удалось обработать картинку;
  - **[1300]** ➱ Не удалось обработать кнопку;
  - **[1400]** ➯ Не удалось обработать файл;
  - **[1500]** ➱ Не удалось обработать действие;

- `**[100_]**` Exeption
  - **[1001]** ➱ Превышение размера файла;
  - **[1002]** ➱ Неподдерживаемый формат файла;
  - **[1003]** ➱ Проблемы с локализацией дней недели;
  - **[1004]** ➱ Ошибка в выполнении инструкции Regex;
  - **[1005]** ➱ Выполнение команды до истечения заданного промежутку времени;
  - **[1006]** ➱ Выполнение команды при Не открытом или Не закрытом соединении;

- `**[200_]**` Exeption
  - **[2001]** ➱ Ошибка работы сети;
  - **[2002]** ➱ Ошибка в использовании Id пользователя;

- `**[300_]**` Exeption
  - **[3001]** ➱ Ошибка в преобразовании текста для БД/Метода;
  - **[3002]** ➱ Значение в базе данных не найдено или равно NULL;
  - **[3003]** ➱ Значение уже находится в Базе данных;
  - **[3004]** ➱ Невозможно явно определить единственно верное значение из БД;

- `**[400_]**` Exeption
  - **[4001]** ➱ Отсутствие файлов;
  - **[4002]** ➱ Одной из значений имеет значение NULL или размер равный 0;
  - **[4003]** ➱ Недостаточно прав для выполнения действия;
> [!NOTE]
> Данные номера ошибок придуманы мной для упрощения выявления ошибок и встречаются в коде и по мере выполнения различных действий.
## История версий
> [!IMPORTANT]
> ### V2.5 - V3.0 (Будущее)
> * ### Нововведения и улучшения:
  > 1) Возможная доработка кода для использования **Пула подключений** <br>
>  [App.config](https://github.com/Liberi/LibDz_infoBot/blob/main/App.config)
>```
> connectionString="....; Pooling=true;"
>```
> _Использование пула подключений может существенно снизить накладные расходы на установку и разрыв соединения при каждом запросе к базе данных, что может улучшит производительность приложения._

> [!TIP]
> ### V2.2 - V2.5 (Текущее)
> * ### Нововведения и улучшения:
  > 1) Исправлена значительная недоработка, при которой основные глобальные переменные были привязаны для всех пользователей, из-за которых происходили непонимания в выполнении некоторых действий.
  > 2) Исправлена значительная недоработка при которой в 90% случаев получения критических ошибок блокировался прием сообщений, теперь в большинстве случаев (по примерным подсчетам) ~80% даже при подобных ошибках будет оставаться возможность использования бота. 
  > 3) Добавлено отслеживание активности Бота, теперь боле понятно, что бот в процессе ответа, а не простаивает.
  >      - Различные действия ответа текстовым сообщением и т.п: **`Печатает…`**
  >      - Различные действия отправки фотографии: **`⋙Загружает фото`**
  >      - Различные действия отправки фотографии+текста: **`⋙Загружает документ`**
  >      - Добавлены пояснения (всплывающие уведомления) при взаимодействии с Inline кнопками.
  > 4) Завершение шагов к выходу на подключение сторонних групп.
  > 5) Визуальная переработка отображения основных данных консоли.
  > 6) Обновлены таймеры "Авто-отправки дз" и "Изменения типа недели"
  > 7) Завершение обновления базы данных.
  >      - Обновление под различные группы;
  >      - Звонки, тип недели теперь поделены по группам;
  >      - Перенос файлов `editDzChatID` и `weekType` в базу;
  >      - Хранение картинки Дз в таблице для каждой группы.
  > 8) Проведено 90% тестов на выявление ошибок. 
  > 9) Проведена дополнительная оптимизация кода.
  >      - Различные улучшения;
  >      - Некоторый код вынесен по классам _(первые шаги к "структуризации" кода)_;
  >      - Картинка из `"🏠Текущее Дз"` теперь не генерируется каждый раз, а берется из Бд;
  >      - При редактировании, добавлении, удалении дз если нечего редактировать в основном чате, Дз не генерируется впустую.  
  > 10) Добавлены/исправлены функции (ч2), которые могут ускорить взаимодействие.  
  >      - Значительно облегчено добавление звонков, теперь не так требовательно к написанию;
  >      - Добавление дз и некоторое добавление расписания и новостей теперь не использует разделители;
  >      - Раздел `"🗓Расписания"` перемещен на главную;
  >      - Разделы `"📰Управление новостями"` и `"🤖Управление ботом"` вынесены на следующий уровень.
  > 11) Добавлены новые [команды](#актуальные-команды), обновлен список [ошибок](#список-ошибок)
  >      - Добавлена возможность отправлять дз в чат не по таймеру: `/chat_send_dz`
  >      - Добавлена возможность отменить авто-отправку дз на неопределенный срок: `/update_send_dz`
  > 12) _Добавлена возможность добавить замены пар или полностью поменять все пары для Дз._
  > 13) Теперь кнопка под сообщением **`"🖼В виде картинки"`** выводит именно те данные под которыми она находилась _(последние 5 сообщений с данной кнопкой)_, а не последнее под чем она появлялась.
  > 14) Убран моментальный бан за спам => Заменено на временный мут на 10мин.
  > 15) Добавлено подтверждение перед удалением звонков.
  > 16) Все необходимые папки для работы бота вынесены в начальную папку.
  > 17) Первые попытки выхода на внешние сервера:
  >      - Первое размещение на сервере **Windows Server 2012 R2** _(пробный период, бот стоял около 3х дней)_
  >      - Переход на сервер **Windows Server Core 2019** _(стадия тестирования...)_

> ### V2.2
> * ### Нововведения и улучшения:
  > 1) Начинаются первые шаги к выходу на подключение сторонних групп.
  > 2) В большей ее части изменена база данных.
  > 3) Проведена большая часть тестов на выявление ошибок. 
  > 4) Проведена небольшая оптимизация кода.
  > 5) Добавлены/исправлены функции (ч1), которые могут ускорить взаимодействие.
  > 6) Теперь человека не банит сразу после включения бота если он прислал много сообщений в его бездействие.
  > 7) Автоматическое прекращение работы других ботов, при конфликте, запущенных недавно.

> ### V2.1
> Проведена расширенная оптимизация кода, добавлены функции, ускоряющие взаимодействие. 

> ### V2.0
> Добавлены новые функции и проведена оптимизация кода.
> * ### Нововведения и улучшения:
  > 1) Теперь вы можете узнать сколько осталось времени до конца пары или до ее начала. Для этого перейдите во вкладку: Ещё → Расписания → До конца пары.
  > 2) Теперь вы можете посмотреть текущее ДЗ до 10:00, после вам будет показываться ДЗ на завтрашний день.

> ### V1.1 - V1.9
> Запуск бота в работу, различные доработки и исправление ошибок. Добавление/исправление функций по желаниям пользователей.

> ### V0.1 - V1.0
> Начало разработки, внедрение первых функций и тестирование в реальных условиях. Проведены тесты на выявление ошибок.
