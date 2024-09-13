package main

import (
	"crypto/md5"
	"database/sql"
	"fmt"
	"log"
	"net/http"
	"time"

	"github.com/gin-contrib/cors"
	"github.com/gin-gonic/gin"
	_ "github.com/go-sql-driver/mysql"
	"golang.org/x/crypto/bcrypt"
)

// sw_downgrad_udp sw_downgrad_udp2 sw_downgrad_udp3 sw_downgrad_udp4 sw_downgrad_udp5

// const (
// 	MYSQL_USER     = "root"
// 	MYSQL_PASSWORD = "kvd19212245"
// 	MYSQL_HOST     = "localhost"
// 	MYSQL_DATABASE = "DOWNGRADVPN"
// )

const (
	MYSQL_USER     = "shit"
	MYSQL_PASSWORD = "carln109"
	MYSQL_HOST     = "147.45.77.19"
	MYSQL_DATABASE = "DOWNGRADVPN"
)

type Server struct {
	ServerID      int    `json:"server_id"`
	ServerCountry string `json:"server_country"`
	ServerName    string `json:"server_name"`
	TotalClients  int    `json:"total_clients"`
	UsedClients   int    `json:"used_clients"`
}

type User struct {
	ID       int    `json:"id"`
	Email    string `json:"email"`
	Password string `json:"password"`
}

type Client struct {
	CommonName  string `json:"common_name"`
	IsConnected bool   `json:"is_connected"`
}

func loginUser(c *gin.Context) {
	var input User
	if err := c.ShouldBindJSON(&input); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}
	fmt.Print(input)

	dsn := fmt.Sprintf("%s:%s@tcp(%s:3306)/%s", MYSQL_USER, MYSQL_PASSWORD, MYSQL_HOST, MYSQL_DATABASE)
	db, err := sql.Open("mysql", dsn)
	if err != nil {
		log.Fatal(err)
	}
	defer db.Close()

	var storedUser User
	err = db.QueryRow("SELECT USER_ID, USER_EMAIL, USER_PASSWORD FROM USERS WHERE USER_EMAIL = ?", input.Email).Scan(&storedUser.ID, &storedUser.Email, &storedUser.Password)
	if err != nil {
		if err == sql.ErrNoRows {
			c.JSON(http.StatusUnauthorized, gin.H{"error": "Invalid email or password"})
		} else {
			c.JSON(http.StatusInternalServerError, gin.H{"error": "Database error"})
		}
		return
	}

	err = bcrypt.CompareHashAndPassword([]byte(storedUser.Password), []byte(input.Password))
	if err != nil {
		c.JSON(http.StatusUnauthorized, gin.H{"error": "Invalid email or password"})
		return
	}

	// Generate session key
	sessionKey := generateSessionKey()

	// Update session key in the database
	_, err = db.Exec("UPDATE USERS SET APP_SESSION_KEY = ? WHERE USER_ID = ?", sessionKey, storedUser.ID)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to update session key"})
		return
	}

	// Return user data and session key on successful login
	c.JSON(http.StatusOK, gin.H{
		"message":     "Login successful",
		"user_id":     storedUser.ID,
		"user_email":  storedUser.Email,
		"session_key": sessionKey,
	})
}

func generateSessionKey() string {
	// Example: generate a random session key
	return fmt.Sprintf("%x", md5.Sum([]byte(time.Now().String())))
}

func getUserServers(c *gin.Context) {
	var requestBody struct {
		UserID int `json:"user_id"`
	}

	// Parse the JSON request body
	if err := c.ShouldBindJSON(&requestBody); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "Invalid request body"})
		return
	}

	userID := requestBody.UserID

	// MySQL database connection string
	dsn := fmt.Sprintf("%s:%s@tcp(%s:3306)/%s", MYSQL_USER, MYSQL_PASSWORD, MYSQL_HOST, MYSQL_DATABASE)
	db, err := sql.Open("mysql", dsn)
	if err != nil {
		log.Fatal(err)
	}
	defer db.Close()

	// Query to fetch the servers for the user ID
	query := `
        SELECT 
            SERVERS.SERVER_ID, 
            SERVERS.SERVER_COUNTRY, 
            SERVERS.SERVER_NAME,
            (SELECT COUNT(*) FROM SERVER_CLIENTS WHERE SERVER_ID = SERVERS.SERVER_ID) AS total_clients,
            (SELECT COUNT(*) FROM SERVER_CLIENTS WHERE SERVER_ID = SERVERS.SERVER_ID AND IS_USED = 1) AS used_clients
        FROM SERVERS
        WHERE SERVERS.USER_ID = ?
    `
	rows, err := db.Query(query, userID)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to fetch servers"})
		return
	}
	defer rows.Close()

	var servers []Server
	for rows.Next() {
		var server Server
		err := rows.Scan(&server.ServerID, &server.ServerCountry, &server.ServerName, &server.TotalClients, &server.UsedClients)
		if err != nil {
			c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to fetch server details"})
			return
		}
		servers = append(servers, server)
	}

	// Return the list of servers in JSON format
	c.JSON(http.StatusOK, gin.H{"servers": servers})
}

func releaseClient(c *gin.Context) {
	clientID := c.Param("clientID")
	fmt.Println(clientID)
	dsn := fmt.Sprintf("%s:%s@tcp(%s:3306)/%s", MYSQL_USER, MYSQL_PASSWORD, MYSQL_HOST, MYSQL_DATABASE)
	db, err := sql.Open("mysql", dsn)
	if err != nil {
		log.Fatal(err)
	}
	defer db.Close()

	updateQuery := `UPDATE SERVER_CLIENTS SET IS_USED = 0 WHERE CLIENT_ID = ?`
	_, err = db.Exec(updateQuery, clientID) // Use = instead of := here
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to release client"})
		return
	}

	c.JSON(http.StatusOK, gin.H{"message": "Client released"})
}

func main() {

	r := gin.Default()

	r.POST("/servers", getUserServers)

	r.Use(cors.New(cors.Config{
		AllowOrigins: []string{"http://localhost:3000"},
		AllowMethods: []string{"GET", "POST", "PUT", "DELETE"},
		AllowHeaders: []string{"Origin", "Content-Type", "Authorization"},
	}))
	r.POST("/login", loginUser)
	// убрать этот кринж ебаный и заменить примером с "/downloadfreeclient/:serverID"
	r.GET("/download/:clientID", func(c *gin.Context) {
		clientID := c.Param("clientID")

		dsn := fmt.Sprintf("%s:%s@tcp(%s:3306)/%s", MYSQL_USER, MYSQL_PASSWORD, MYSQL_HOST, MYSQL_DATABASE)
		db, err := sql.Open("mysql", dsn)
		if err != nil {
			log.Fatalf("Failed to connect to database: %v", err)
		}
		defer db.Close()

		var clientName string
		query := `SELECT CLIENT_NAME FROM SERVER_CLIENTS WHERE CLIENT_ID = ?`
		err = db.QueryRow(query, clientID).Scan(&clientName)
		if err != nil {
			if err == sql.ErrNoRows {
				c.JSON(http.StatusNotFound, gin.H{"error": "Client not found"})
			} else {
				c.JSON(http.StatusInternalServerError, gin.H{"error": "Database error"})
			}
			return
		}

		c.JSON(http.StatusOK, gin.H{"clientName": clientName})
	})

	r.GET("/downloadfreeclient/:serverID", func(c *gin.Context) {
		serverID := c.Param("serverID")

		dsn := fmt.Sprintf("%s:%s@tcp(%s:3306)/%s", MYSQL_USER, MYSQL_PASSWORD, MYSQL_HOST, MYSQL_DATABASE)
		db, err := sql.Open("mysql", dsn)
		if err != nil {
			log.Fatalf("Failed to connect to database: %v", err)
		}
		defer db.Close()

		var clientID int
		var clientName string
		query := `SELECT CLIENT_ID, CLIENT_NAME FROM SERVER_CLIENTS WHERE SERVER_ID = ? AND IS_USED = 0 LIMIT 1`
		err = db.QueryRow(query, serverID).Scan(&clientID, &clientName)
		if err != nil {
			if err == sql.ErrNoRows {
				c.JSON(http.StatusNotFound, gin.H{"error": "No free clients available"})
			} else {
				c.JSON(http.StatusInternalServerError, gin.H{"error": "Database error"})
			}
			return
		}

		updateQuery := `UPDATE SERVER_CLIENTS SET IS_USED = 1 WHERE CLIENT_ID = ?`
		_, err = db.Exec(updateQuery, clientID)
		if err != nil {
			c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to update client status"})
			return
		}

		// Add the CLIENT_ID to the header
		c.Header("X-Client-ID", fmt.Sprintf("%d", clientID))

		// Send the corresponding client config file
		filePath := fmt.Sprintf("./clients/%s.ovpn", clientName)
		c.File(filePath)
	})

	r.POST("/updateClients", func(c *gin.Context) {
		// Parse the incoming JSON data
		var clients []Client
		if err := c.ShouldBindJSON(&clients); err != nil {
			c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
			return
		}

		// Set up the database connection
		dsn := fmt.Sprintf("%s:%s@tcp(%s:3306)/%s", MYSQL_USER, MYSQL_PASSWORD, MYSQL_HOST, MYSQL_DATABASE)
		db, err := sql.Open("mysql", dsn)
		if err != nil {
			log.Fatalf("Failed to connect to database: %v", err)
			c.JSON(http.StatusInternalServerError, gin.H{"error": "Failed to connect to database"})
			return
		}
		defer db.Close()

		// Process each client in the request
		for _, client := range clients {
			var query string
			if client.IsConnected {
				query = `UPDATE SERVER_CLIENTS SET IS_USED = 1 WHERE CLIENT_NAME = ?`
			} else {
				query = `UPDATE SERVER_CLIENTS SET IS_USED = 0 WHERE CLIENT_NAME = ?`
			}

			// Execute the update query
			_, err := db.Exec(query, client.CommonName)
			if err != nil {
				log.Printf("Failed to update client %s: %v", client.CommonName, err)
				c.JSON(http.StatusInternalServerError, gin.H{"error": fmt.Sprintf("Failed to update client: %s", client.CommonName)})
				return
			}
		}

		// Return success response
		c.JSON(http.StatusOK, gin.H{"message": "Clients updated successfully"})
	})

	r.POST("/releaseclient/:clientID", releaseClient)

	r.Run(":8080")

	// user := "root"
	// password := "carln109"
	// server := "147.45.77.19"
	// remoteFilePath := "/root/SE_DVPNTEST_122.ovpn"
	// localFilePath := "./clients/SE_DVPNTEST_122.ovpn"

	// err := downloadFile(user, password, server, remoteFilePath, localFilePath)
	// if err != nil {
	// 	fmt.Printf("Error: %v\n", err)
	// } else {
	// 	fmt.Println("File downloaded successfully!")
	// }
	// r := gin.Default()

	// r.POST("/addserver", addServer)

	// r.Run(":8080")
}

// type Server struct {
// 	IP       string `json:"serverIp"`
// 	Username string `json:"username"`
// 	Password string `json:"password"`
// }

// func sshConnect(user, password, server string) (*ssh.Client, error) {
// 	config := &ssh.ClientConfig{
// 		User: user,
// 		Auth: []ssh.AuthMethod{
// 			ssh.Password(password),
// 		},
// 		HostKeyCallback: ssh.InsecureIgnoreHostKey(),
// 		Timeout:         5 * time.Second,
// 	}

// 	client, err := ssh.Dial("tcp", server+":22", config)
// 	if err != nil {
// 		return nil, fmt.Errorf("failed to dial: %v", err)
// 	}

// 	return client, nil
// }

// func downloadFile(user, password, server, remoteFilePath, localFilePath string) error {
// 	client, err := sshConnect(user, password, server)
// 	if err != nil {
// 		return fmt.Errorf("failed to connect: %v", err)
// 	}
// 	defer client.Close()

// 	// Create an SFTP session
// 	sftpClient, err := sftp.NewClient(client)
// 	if err != nil {
// 		return fmt.Errorf("failed to create sftp client: %v", err)
// 	}
// 	defer sftpClient.Close()

// 	// Ensure the local directory exists
// 	localDir := filepath.Dir(localFilePath)
// 	if _, err := os.Stat(localDir); os.IsNotExist(err) {
// 		err := os.MkdirAll(localDir, 0755)
// 		if err != nil {
// 			return fmt.Errorf("failed to create directory: %v", err)
// 		}
// 	}

// 	// Open the remote file
// 	remoteFile, err := sftpClient.Open(remoteFilePath)
// 	if err != nil {
// 		return fmt.Errorf("failed to open remote file: %v", err)
// 	}
// 	defer remoteFile.Close()

// 	// Create the local file
// 	localFile, err := os.Create(localFilePath)
// 	if err != nil {
// 		return fmt.Errorf("failed to create local file: %v", err)
// 	}
// 	defer localFile.Close()

// 	// Copy the contents of the remote file to the local file
// 	_, err = remoteFile.WriteTo(localFile)
// 	if err != nil {
// 		return fmt.Errorf("failed to write to local file: %v", err)
// 	}

// 	return nil
// }

// func addServer(c *gin.Context) {
// 	var input Server
// 	if err := c.ShouldBindJSON(&input); err != nil {
// 		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
// 		return
// 	}

// 	fmt.Println(input)
// 	installOpenVPN(input.IP, input.Username, input.Password)

// 	c.JSON(http.StatusCreated, gin.H{"message": "Openvpn has been installed"})

// }

// func installOpenVPN(server string, user string, password string) {
// 	{
// 		session, err := sshConnect(user, password, server)
// 		if err != nil {
// 			log.Fatalf("SSH connection failed: %v", err)
// 		}
// 		defer session.Close()

// 		// Concatenate commands to run in a single SSH session
// 		commands := `curl -O https://raw.githubusercontent.com/angristan/openvpn-install/master/openvpn-install.sh && \
// 		chmod +x openvpn-install.sh && \
// 		export AUTO_INSTALL=y && ./openvpn-install.sh`

// 		err = runRemoteCommand(session, commands)
// 		if err != nil {
// 			log.Fatalf("Error running command: %v", err)
// 		}

// 		fmt.Println("OpenVPN setup completed successfully on the remote server!")
// 	}
// }

// func runRemoteCommand(session *ssh.Session, command string) error {
// 	fmt.Printf("Executing command: %s\n", command)
// 	cmdOutput, err := session.CombinedOutput(command)
// 	if err != nil {
// 		return fmt.Errorf("failed to run command: %v", err)
// 	}
// 	fmt.Println("Command output:", string(cmdOutput))
// 	return nil
// }
