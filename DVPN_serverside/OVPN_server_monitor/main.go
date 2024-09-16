package main

import (
	"bufio"
	"bytes"
	"encoding/json"
	"fmt"
	"net/http"
	"os"
	"strings"
	"time"
)

type Client struct {
	CommonName  string `json:"common_name"`
	IsConnected bool   `json:"is_connected"`
}

var previousClients = map[string]bool{}

func readStatusLog(filePath string) (map[string]bool, error) {
	file, err := os.Open(filePath)
	if err != nil {
		return nil, err
	}
	defer file.Close()

	scanner := bufio.NewScanner(file)
	clients := make(map[string]bool)
	inClientList := false

	for scanner.Scan() {
		line := scanner.Text()

		if strings.HasPrefix(line, "Common Name") {
			inClientList = true
			continue
		}
		if strings.HasPrefix(line, "ROUTING TABLE") {
			break
		}
		if !inClientList || strings.TrimSpace(line) == "" {
			continue
		}

		fields := strings.Split(line, ",")
		if len(fields) >= 5 {
			clientName := fields[0]
			clients[clientName] = true
		}
	}

	if err := scanner.Err(); err != nil {
		return nil, err
	}

	return clients, nil
}

func sendUpdatesToServer(changedClients []Client) error {

	//      Server URL
	url := "https://downgrad.com/api/updateClients"

	jsonData, err := json.Marshal(changedClients)
	if err != nil {
		return err
	}

	req, err := http.NewRequest("POST", url, bytes.NewBuffer(jsonData))
	if err != nil {
		return err
	}
	req.Header.Set("Content-Type", "application/json")

	client := &http.Client{}
	resp, err := client.Do(req)
	if err != nil {
		return err
	}
	defer resp.Body.Close()

	if resp.StatusCode != http.StatusOK {
		return fmt.Errorf("failed to send updates: %s", resp.Status)
	}

	return nil
}

func monitorClients(logFilePath string) {
	for {
		currentClients, err := readStatusLog(logFilePath)
		if err != nil {
			fmt.Println("Error reading log:", err)
			continue
		}

		changedClients := []Client{}

		for clientName := range currentClients {
			if !previousClients[clientName] {
				changedClients = append(changedClients, Client{CommonName: clientName, IsConnected: true})
			}
		}
		for clientName := range previousClients {
			if !currentClients[clientName] {
				changedClients = append(changedClients, Client{CommonName: clientName, IsConnected: false})
			}
		}

		if len(changedClients) > 0 {
			err = sendUpdatesToServer(changedClients)
			if err != nil {
				fmt.Println("Failed to send updates:", err)
			} else {
				fmt.Println("Sent updates to server.")
			}
		}

		previousClients = currentClients

		//         Monitoring Interval
		time.Sleep(30 * time.Second)
	}
}

func main() {
	//              Log File Path
	monitorClients("/var/log/openvpn/status.log")
}
