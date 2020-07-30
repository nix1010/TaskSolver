CREATE DATABASE programming_tasks;
GO

USE programming_tasks;
GO

CREATE TABLE users(
	id INT IDENTITY(1, 1),
	username VARCHAR(255),
	password VARCHAR(512),
	PRIMARY KEY(id)
);
GO

CREATE TABLE tasks(
	id INT IDENTITY(1, 1),
	title VARCHAR(255),
	description TEXT,
	PRIMARY KEY(id)
);
GO

CREATE TABLE examples(
	id INT IDENTITY(1, 1),
	task_id INT,
	input VARCHAR(255),
	output VARCHAR(255),
	PRIMARY KEY(id),
	FOREIGN KEY(task_id) REFERENCES tasks(id) ON UPDATE CASCADE ON DELETE CASCADE
);
GO

CREATE TABLE users_solutions(
	id INT IDENTITY(1, 1),
	user_id INT,
	task_id INT,
	code VARCHAR(MAX),
	status BIT,
	description VARCHAR(255),
	PRIMARY KEY(id),
	FOREIGN KEY(user_id) REFERENCES users(id) ON UPDATE CASCADE ON DELETE CASCADE,
	FOREIGN KEY(task_id) REFERENCES tasks(id) ON UPDATE CASCADE ON DELETE CASCADE
);

-- tasks DATA
INSERT INTO tasks (title, description) VALUES('Najveci element', 'Napisati funkciju koja ce prihvatiti dva paremtra i vratiti veci element');
INSERT INTO tasks (title, description) VALUES('Najmanji element', 'Napisati funkciju koja ce prihvatiti dva paremtra i vratiti manji element');

-- examples DATA
INSERT INTO examples (task_id, input, output) VALUES(1, '1;2', '2');
INSERT INTO examples (task_id, input, output) VALUES(1, '45;7', '45');

INSERT INTO examples (task_id, input, output) VALUES(2, '7;14', '7');
INSERT INTO examples (task_id, input, output) VALUES(2, '-9;12', '-9');
INSERT INTO examples (task_id, input, output) VALUES(2, '63;15', '15');

-- users DATA
INSERT INTO users (username, password) VALUES('user1', HASHBYTES('SHA2_256', 'user1'));
INSERT INTO users (username, password) VALUES('user2', HASHBYTES('SHA2_256', 'user2'));

SELECT * FROM tasks;
SELECT * FROM users;
SELECT * FROM users_solutions;
exec sp_columns users;



UPDATE users SET password = HASHBYTES('SHA2_256', 'user2') WHERE username = 'user2';

SELECT HASHBYTES('SHA2_256', 'user1');

ALTER TABLE users DROP COLUMN password;
ALTER TABLE users ADD password VARCHAR(512);