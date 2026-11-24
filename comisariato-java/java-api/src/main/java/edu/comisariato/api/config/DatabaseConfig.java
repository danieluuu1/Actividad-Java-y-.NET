package edu.comisariato.api.config;
import javax.sql.DataSource;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.core.Ordered;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.retry.annotation.EnableRetry;
import org.springframework.transaction.annotation.EnableTransactionManagement;
@Configuration
// Retry envuelve a Transaction: cada intento abre una transaccion nueva.
@EnableRetry(order = Ordered.LOWEST_PRECEDENCE - 1)
@EnableTransactionManagement(order = Ordered.LOWEST_PRECEDENCE)
public class DatabaseConfig {
    @Bean public JdbcTemplate jdbcTemplate(DataSource dataSource) {
        JdbcTemplate jdbc = new JdbcTemplate(dataSource);
        jdbc.setQueryTimeout(30);
        return jdbc;
    }
}
