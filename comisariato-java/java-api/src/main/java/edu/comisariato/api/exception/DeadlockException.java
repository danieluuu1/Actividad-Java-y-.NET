package edu.comisariato.api.exception;
public class DeadlockException extends RuntimeException {
    public DeadlockException(Throwable cause) { super("SQL Server detecto un deadlock (1205)", cause); }
}
