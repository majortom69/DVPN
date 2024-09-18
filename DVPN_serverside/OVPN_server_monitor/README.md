# OpenVPN Server Monitor

## English

This program monitors the connected clients on an OpenVPN server by reading the `status.log` file. Whenever a client connects or disconnects, the program detects the change and sends a POST request to the main server to update the client table with the new data.

### Features:
- Monitors OpenVPN `status.log` file for changes.
- Detects new connections and disconnections.
- Sends updates to the main server in JSON format.

### Configuration:
1. **Server URL**: The POST request is sent to a specific URL.
2. **Log File Path**: The path to the OpenVPN `status.log` file.
3. **Monitoring Interval**: The time interval for checking the log file.

### Usage:
1. Install Go on your machine.
2. Adjust the server URL, log file path(if needed), and monitoring interval.
3. Compile and run the program:
   ```bash
   go build main.go
   chmod +x main
   nohup ./main &

## Русский

Программа для мониторинга подключений клиентов к серверу OpenVPN, которая работает за счет считывания данных из файла status.log. Когда клиент подключается или отключается, программа фиксирует изменения и отправляет POST-запрос на основной сервер для обновления информации о клиентах.

### Основные функции:
- Отслеживание изменений в файле `status.log` OpenVPN.
- Обнаружение новых подключений и отключений клиентов.
- Отправка обновлений на основной сервер в формате JSON.

### Конфигурация:
1. **Server URL**: POST-запрос отправляется на указанный URL.
2. **Log File Path**: Путь к файлу status.log OpenVPN.
3. **Monitoring Interval**: Интервал между проверками логов на изменения.

### Инструкции:
1. Установите Go на ваш комьютер.
2. Настройте URL сервера, путь к файлу c логами(если требуется) и интервал проверки.
3. Скомпилируйте и запустите программу:
   ```bash
   go build main.go
   chmod +x main
   nohup ./main &